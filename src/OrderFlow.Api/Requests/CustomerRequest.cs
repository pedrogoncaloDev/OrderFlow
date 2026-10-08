using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Api.Requests;

/// <summary>Corpo de POST /api/customers e PUT /api/customers/{id}.</summary>
public sealed class CustomerRequest : IValidatableObject
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(320, ErrorMessage = "O e-mail deve ter no máximo 320 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [StringLength(30, ErrorMessage = "O telefone deve ter no máximo 30 caracteres.")]
    public string? Phone { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Nome só com espaços passa no [Required] (o JSON não é aparado); a validação confere de novo.
        if (!string.IsNullOrEmpty(Name) && string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Informe o nome.", [nameof(Name)]);
    }
}
