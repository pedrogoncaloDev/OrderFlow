namespace OrderFlow.Application.Auth;

/// <summary>
/// Forma canônica do e-mail (sem espaços nas pontas e em minúsculas). Todo código que grava ou
/// procura usuário por e-mail deve passar por aqui, senão surgem contas duplicadas ou logins que falham.
/// </summary>
public static class EmailNormalizer
{
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
