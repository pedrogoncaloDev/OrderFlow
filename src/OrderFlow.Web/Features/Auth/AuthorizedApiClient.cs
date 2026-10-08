using System.Net;
using OrderFlow.Web.Common;

namespace OrderFlow.Web.Features.Auth;

/// <summary>
/// Base dos clientes que exigem login: pega o token da sessão atual a cada chamada e, se a API
/// o recusar (401), encerra a sessão local. Quem chama só precisa mandar o usuário para o login.
/// </summary>
public abstract class AuthorizedApiClient : ApiClientBase
{
    private readonly JwtAuthenticationStateProvider _auth;

    // Construtor
    protected AuthorizedApiClient(HttpClient http, JwtAuthenticationStateProvider auth) : base(http)
    {
        _auth = auth;
    }

    protected Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken cancellationToken) =>
        AuthorizedAsync((token) => SendAsync<T>(HttpMethod.Get, url, null, token, cancellationToken));

    protected Task<ApiResult<T>> PostAsync<T>(string url, object body, CancellationToken cancellationToken) =>
        AuthorizedAsync((token) => SendAsync<T>(HttpMethod.Post, url, body, token, cancellationToken));

    protected Task<ApiResult<T>> PutAsync<T>(string url, object body, CancellationToken cancellationToken) =>
        AuthorizedAsync((token) => SendAsync<T>(HttpMethod.Put, url, body, token, cancellationToken));

    protected Task<ApiResult<bool>> DeleteAsync(string url, CancellationToken cancellationToken) =>
        AuthorizedAsync((token) => SendWithoutBodyAsync(HttpMethod.Delete, url, null, token, cancellationToken));

    private async Task<ApiResult<T>> AuthorizedAsync<T>(Func<string, Task<ApiResult<T>>> send)
    {
        var token = await _auth.GetAccessTokenAsync();

        if (token is null)
            return ApiResult<T>.Fail("Sua sessão expirou. Entre novamente.", HttpStatusCode.Unauthorized);

        var result = await send(token);

        if (result.IsUnauthorized)
            await _auth.SignOutAsync(); // token recusado (expirado ou chave trocada): descarta a sessão local

        return result;
    }
}
