namespace OrderFlow.Application.Auth;

/// <summary>Dados públicos do usuário. Nunca inclui o hash da senha.</summary>
public sealed record UserResponse(Guid Id, string Email, string Role, DateTime CreatedAt);

/// <summary>Resposta de cadastro e login: o token JWT e o usuário autenticado.</summary>
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserResponse User);
