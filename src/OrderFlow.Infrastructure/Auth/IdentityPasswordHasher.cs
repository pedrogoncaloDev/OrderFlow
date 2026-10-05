using Microsoft.AspNetCore.Identity;

namespace OrderFlow.Infrastructure.Auth;

/// <summary>
/// Hash de senha com o PasswordHasher do ASP.NET Core Identity (PBKDF2 com salt aleatório por senha).
/// </summary>
public sealed class IdentityPasswordHasher
{
    // O hasher padrão não usa o objeto "usuário"; um sentinela evita acoplar este código ao Domain.
    private static readonly object Subject = new();

    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(Subject, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(Subject, passwordHash, password) != PasswordVerificationResult.Failed;
}
