namespace OrderFlow.Application.Auth;

public enum AuthErrorCode
{
    EmailAlreadyRegistered,
    InvalidCredentials
}

/// <summary>
/// Resultado de cadastro/login. Falhas esperadas (e-mail repetido, senha errada) não são
/// exceções: viram um código de erro que a API traduz para o status HTTP adequado.
/// </summary>
public sealed record AuthResult
{
    public AuthResponse? Response { get; private init; }
    public AuthErrorCode? Error { get; private init; }

    public bool Succeeded => Response is not null;

    public static AuthResult Success(AuthResponse response) => new() { Response = response };

    public static AuthResult Failure(AuthErrorCode error) => new() { Error = error };
}
