using OrderFlow.Web.Common;

namespace OrderFlow.Web.Auth;

/// <summary>Cliente HTTP da API de autenticação.</summary>
public sealed class AuthApiClient : ApiClientBase
{
    // Construtor
    public AuthApiClient(HttpClient http) : base(http)
    {
    }

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginModel model, CancellationToken cancellationToken = default) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", new { model.Email, model.Password }, null, cancellationToken);

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterModel model, CancellationToken cancellationToken = default) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", new { model.Email, model.Password }, null, cancellationToken);

    public Task<ApiResult<UserInfo>> GetMeAsync(string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync<UserInfo>(HttpMethod.Get, "api/auth/me", null, accessToken, cancellationToken);
}
