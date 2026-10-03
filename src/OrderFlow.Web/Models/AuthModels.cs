using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Web.Models;

/// <summary>Formulário de login.</summary>
public sealed class LoginModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Formulário de cadastro. As regras de senha espelham as da API (PasswordPolicy, em OrderFlow.Application);
/// a API continua sendo a fonte da verdade e valida tudo de novo.
/// </summary>
public sealed class RegisterModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(320, ErrorMessage = "O e-mail deve ter no máximo 320 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre 8 e 100 caracteres.")]
    [RegularExpression(@"^(?=.*\p{L})(?=.*\d).+$", ErrorMessage = "A senha deve conter letras e números.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [Compare(nameof(Password), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>Dados do usuário devolvidos pela API.</summary>
public sealed record UserInfo(Guid Id, string Email, string Role, DateTime CreatedAt);

/// <summary>Resposta de cadastro e login da API.</summary>
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserInfo User);

/// <summary>Sessão guardada (criptografada) no navegador.</summary>
public sealed record StoredSession(string AccessToken, DateTime ExpiresAtUtc);
