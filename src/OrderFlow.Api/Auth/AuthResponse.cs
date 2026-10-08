namespace OrderFlow.Api.Auth;

/// <summary>Resposta de POST /api/auth/register e /api/auth/login: { accessToken, expiresAtUtc, user }.</summary>
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserResponse User);
