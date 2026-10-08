using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OrderFlow.Application.Auth;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Auth;

public sealed class JwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;

    // Construtor
    public JwtTokenGenerator(JwtOptions options, TimeProvider clock)
    {
        _options = options;
        _clock = clock;
    }

    public (string Value, DateTime ExpiresAtUtc) Generate(User user)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(_options.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(AuthClaimTypes.Subject, user.Id.ToString()),
            new Claim(AuthClaimTypes.Email, user.Email),
            new Claim(AuthClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
