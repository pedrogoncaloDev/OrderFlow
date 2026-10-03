using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using OrderFlow.Web.Models;

namespace OrderFlow.Web.Services;

/// <summary>Resultado de uma chamada à API: ou o valor, ou uma mensagem de erro pronta para exibir.</summary>
public sealed record ApiResult<T>(bool Succeeded, T? Value, string? Error, HttpStatusCode? StatusCode)
{
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;

    public static ApiResult<T> Ok(T value) => new(true, value, null, null);

    public static ApiResult<T> Fail(string error, HttpStatusCode? statusCode = null) =>
        new(false, default, error, statusCode);
}

/// <summary>Cliente HTTP da API de autenticação.</summary>
public sealed class AuthApiClient
{
    private readonly HttpClient _http;

    // Construtor
    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginModel model, CancellationToken cancellationToken = default) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", new { model.Email, model.Password }, null, cancellationToken);

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterModel model, CancellationToken cancellationToken = default) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", new { model.Email, model.Password }, null, cancellationToken);

    public Task<ApiResult<UserInfo>> GetMeAsync(string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync<UserInfo>(HttpMethod.Get, "api/auth/me", null, accessToken, cancellationToken);

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        string? accessToken,
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

            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);

            return value is null
                ? ApiResult<T>.Fail("A API retornou uma resposta vazia.", response.StatusCode)
                : ApiResult<T>.Ok(value);
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
    internal sealed record ApiProblem(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}
