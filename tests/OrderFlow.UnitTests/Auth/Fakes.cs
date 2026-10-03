using OrderFlow.Application.Auth;
using OrderFlow.Domain.Entities;

namespace OrderFlow.UnitTests.Auth;

/// <summary>Repositório em memória para testar o AuthService sem banco de dados.</summary>
internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    /// <summary>Simula a violação do índice único (duas requisições com o mesmo e-mail ao mesmo tempo).</summary>
    public bool TryAddShouldFail { get; set; }

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == normalizedEmail));

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<bool> TryAddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (TryAddShouldFail)
            return Task.FromResult(false);

        Users.Add(user);
        return Task.FromResult(true);
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;

    public bool Verify(string passwordHash, string password) => passwordHash == "hash:" + password;
}

internal sealed class FakeTokenGenerator : ITokenGenerator
{
    public static readonly DateTime ExpiresAt = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public GeneratedToken Generate(User user) => new("fake-token", ExpiresAt);
}
