namespace OrderFlow.Application.Auth;

/// <summary>
/// Regras de senha. O front-end (OrderFlow.Web) repete estes valores em seu formulário;
/// ao mudar algo aqui, ajuste lá também.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    // Limite máximo evita que senhas gigantes sirvam para sobrecarregar o algoritmo de hash.
    public const int MaxLength = 100;

    // Pelo menos uma letra e um número.
    public const string Pattern = @"^(?=.*\p{L})(?=.*\d).+$";
}
