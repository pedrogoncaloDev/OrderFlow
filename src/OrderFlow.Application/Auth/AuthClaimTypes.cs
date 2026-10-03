namespace OrderFlow.Application.Auth;

/// <summary>
/// Nomes das claims do JWT. Usados na emissão do token (Infrastructure) e na sua validação (Api).
/// </summary>
public static class AuthClaimTypes
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Role = "role";
}
