using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Api.Features.Products;

/// <summary>Corpo de POST /api/products e PUT /api/products/{id}.</summary>
public sealed class ProductRequest : IValidatableObject
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
    public string? Description { get; init; }

    [Range(0, 1_000_000_000, ErrorMessage = "O preço deve estar entre 0 e 1.000.000.000.")]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo.")]
    public int StockQuantity { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // A coluna é numeric(18,2): mais de duas casas seriam arredondadas em silêncio.
        if (Price != decimal.Round(Price, 2))
            yield return new ValidationResult("O preço deve ter no máximo 2 casas decimais.", [nameof(Price)]);

        // Nome só com espaços passa no [Required] se o binder não aparar; a validação confere de novo.
        if (!string.IsNullOrEmpty(Name) && string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Informe o nome.", [nameof(Name)]);
    }
}
