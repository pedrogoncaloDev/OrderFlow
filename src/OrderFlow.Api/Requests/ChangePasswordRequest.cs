using System.ComponentModel.DataAnnotations;
using OrderFlow.Application.Auth;

namespace OrderFlow.Api.Requests;

/// <summary>Corpo de POST /api/auth/change_password.</summary>
public sealed class ChangePasswordRequest
{
    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(PasswordPolicy.MaxLength, MinimumLength = PasswordPolicy.MinLength,
        ErrorMessage = "A senha deve ter entre 8 e 100 caracteres.")]
    [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = "A senha deve conter letras e números.")]
    public string old_password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(PasswordPolicy.MaxLength, MinimumLength = PasswordPolicy.MinLength,
        ErrorMessage = "A senha deve ter entre 8 e 100 caracteres.")]
    [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = "A senha deve conter letras e números.")]
    public string new_password { get; init; } = string.Empty;
}
