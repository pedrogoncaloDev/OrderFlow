using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Auth;

/// <summary>Acesso a usuários. A implementação (EF Core) fica em OrderFlow.Infrastructure.</summary>
public interface IUserRepository
{
    /// <param name="normalizedEmail">E-mail já em minúsculas e sem espaços nas pontas.</param>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Salva o usuário. Retorna <c>false</c> se o e-mail já existir (violação do índice único).</summary>
    Task<bool> TryAddAsync(User user, CancellationToken cancellationToken = default);
}
