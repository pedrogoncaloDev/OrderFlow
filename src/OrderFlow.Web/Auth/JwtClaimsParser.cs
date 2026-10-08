using System.Security.Claims;
using System.Text.Json;

namespace OrderFlow.Web.Auth;

/// <summary>
/// Lê as claims do payload de um JWT. Atenção: a assinatura NÃO é verificada aqui (o front-end não tem a chave).
/// Serve só para exibir dados na interface; quem decide o que o usuário pode fazer é sempre a API.
/// </summary>
internal static class JwtClaimsParser
{
    /// <exception cref="FormatException">Token fora do formato JWT.</exception>
    /// <exception cref="JsonException">Payload que não é um JSON válido.</exception>
    public static IReadOnlyList<Claim> Parse(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            throw new FormatException("O token não está no formato JWT.");

        var base64 = parts[1].Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        using var document = JsonDocument.Parse(Convert.FromBase64String(base64));

        var claims = new List<Claim>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in property.Value.EnumerateArray())
                    claims.Add(new Claim(property.Name, item.ToString()));
            }
            else
            {
                claims.Add(new Claim(property.Name, property.Value.ToString()));
            }
        }

        return claims;
    }
}
