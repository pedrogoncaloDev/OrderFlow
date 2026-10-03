using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Application.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [StringLength(320, ErrorMessage = "O e-mail deve ter no máximo 320 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(PasswordPolicy.MaxLength, ErrorMessage = "A senha deve ter no máximo 100 caracteres.")]
    public string Password { get; init; } = string.Empty;
}
