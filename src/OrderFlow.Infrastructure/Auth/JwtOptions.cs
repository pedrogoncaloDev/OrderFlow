namespace OrderFlow.Infrastructure.Auth;

/// <summary>Configurações do JWT (seção "Jwt"). A chave nunca vai em arquivo versionado.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "OrderFlow.Api";

    public string Audience { get; set; } = "OrderFlow.Web";

    /// <summary>Chave de assinatura (HMAC-SHA256). Mínimo de 32 caracteres.</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;
}
