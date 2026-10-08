using System.Net.Http.Headers;
using System.Text.Json;

namespace OrderFlow.Web.Common;

/// <summary>Base dos clientes HTTP da API: envia a requisição e traduz falhas em mensagens para o usuário.</summary>
public abstract class ApiClientBase
{
    /// <summary>Nome do HttpClient configurado com o endereço da API (Api:BaseUrl).</summary>
    public const string HttpClientName = "orderflow-api";

    private readonly HttpClient _http;

    // Construtor
    protected ApiClientBase(HttpClient http)
    {
        _http = http;
    }

    /// <summary>Envia a requisição e lê o corpo da resposta como <typeparamref name="T"/>.</summary>
    protected Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        string? accessToken,
        CancellationToken cancellationToken) =>
        ExecuteAsync(method, url, body, accessToken, async response =>
        {
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

            return value is null
                ? ApiResult<T>.Fail("A API retornou uma resposta vazia.", response.StatusCode)
                : ApiResult<T>.Ok(value);
        }, cancellationToken);

    /// <summary>Envia a requisição e ignora o corpo da resposta (por exemplo, 204 No Content).</summary>
    protected Task<ApiResult<bool>> SendWithoutBodyAsync(
        HttpMethod method,
        string url,
        object? body,
        string? accessToken,
        CancellationToken cancellationToken) =>
        ExecuteAsync(method, url, body, accessToken, _ => Task.FromResult(ApiResult<bool>.Ok(true)), cancellationToken);

    private async Task<ApiResult<T>> ExecuteAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        string? accessToken,
        Func<HttpResponseMessage, Task<ApiResult<T>>> readSuccess,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);

            if (body is not null)
                request.Content = JsonContent.Create(body);

            if (accessToken is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return ApiResult<T>.Fail(await ReadErrorAsync(response, cancellationToken), response.StatusCode);

            return await readSuccess(response);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Fail("Não foi possível conectar à API. Verifique se ela está em execução.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult<T>.Fail("A API demorou demais para responder. Tente novamente.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken);

            // 400: ValidationProblemDetails com as mensagens por campo
            if (problem?.Errors is { Count: > 0 })
                return string.Join(" ", problem.Errors.Values.SelectMany(messages => messages));

            if (!string.IsNullOrWhiteSpace(problem?.Detail))
                return problem.Detail;

            if (!string.IsNullOrWhiteSpace(problem?.Title))
                return problem.Title;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Corpo ausente ou que não é JSON: cai na mensagem genérica abaixo.
        }

        return $"Erro inesperado da API (HTTP {(int)response.StatusCode}).";
    }

    /// <summary>Formato de erro (ProblemDetails) devolvido pela API.</summary>
    private sealed record ApiProblem(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
