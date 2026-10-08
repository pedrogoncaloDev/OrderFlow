using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OrderFlow.Application.Auth;
using OrderFlow.Infrastructure.Auth;

namespace OrderFlow.Api.Features.Auth;

public static class AuthExtensions
{
    private const int MinKeyBytes = 32; // HMAC-SHA256 exige chave de pelo menos 256 bits

    public static IServiceCollection AddJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.Key))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "A chave do JWT não foi configurada. Defina a variável de ambiente 'Jwt__Key' (mínimo de 32 caracteres).");
            }

            // Só em desenvolvimento: chave temporária, gerada a cada execução. Os tokens antigos deixam de valer ao reiniciar a API.
            jwt.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            Console.WriteLine("[aviso] 'Jwt__Key' não definida: usando uma chave temporária (as sessões caem ao reiniciar a API).");
        }

        if (Encoding.UTF8.GetByteCount(jwt.Key) < MinKeyBytes)
        {
            throw new InvalidOperationException(
                $"A chave do JWT ('Jwt__Key') deve ter pelo menos {MinKeyBytes} caracteres.");
        }

        services.AddSingleton(jwt);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IdentityPasswordHasher>();
        services.AddSingleton<JwtTokenGenerator>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Mantém os nomes originais das claims ("sub", "email", "role") em vez de convertê-los.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = AuthClaimTypes.Email,
                    RoleClaimType = AuthClaimTypes.Role
                };
            });

        services.AddAuthorization();

        return services;
    }
}
