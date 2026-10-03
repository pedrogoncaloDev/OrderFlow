using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    // Construtor
    public AuthService(IUserRepository users, IPasswordHasher passwordHasher, ITokenGenerator tokenGenerator)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        if (await _users.GetByEmailAsync(email, cancellationToken) is not null)
            return AuthResult.Failure(AuthErrorCode.EmailAlreadyRegistered);

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Customer // o papel é sempre definido no servidor, nunca vem do cliente
        };

        // Cobre a corrida entre duas requisições com o mesmo e-mail: o índice único do banco decide.
        if (!await _users.TryAddAsync(user, cancellationToken))
            return AuthResult.Failure(AuthErrorCode.EmailAlreadyRegistered);

        return AuthResult.Success(BuildResponse(user));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);

        // Mesma resposta para "usuário não existe" e "senha errada", para não revelar quais e-mails têm conta.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
            return AuthResult.Failure(AuthErrorCode.InvalidCredentials);

        return AuthResult.Success(BuildResponse(user));
    }

    public async Task<UserResponse?> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToResponse(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var token = _tokenGenerator.Generate(user);
        return new AuthResponse(token.Value, token.ExpiresAtUtc, ToResponse(user));
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Email, user.Role.ToString(), user.CreatedAt);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
