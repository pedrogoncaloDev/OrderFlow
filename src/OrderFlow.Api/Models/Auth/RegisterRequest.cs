using System.ComponentModel.DataAnnotations;
using OrderFlow.Application.Auth;

namespace OrderFlow.Api.Models.Auth;

/// <summary>Corpo de POST /api/auth/register.</summary>
public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(320, ErrorMessage = "O e-mail deve ter no máximo 320 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(PasswordPolicy.MaxLength, MinimumLength = PasswordPolicy.MinLength,
        ErrorMessage = "A senha deve ter entre 8 e 100 caracteres.")]
    [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = "A senha deve conter letras e números.")]
    public string Password { get; init; } = string.Empty;
}
