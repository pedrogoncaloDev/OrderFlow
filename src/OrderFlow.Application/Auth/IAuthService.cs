namespace OrderFlow.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<UserResponse?> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
}
