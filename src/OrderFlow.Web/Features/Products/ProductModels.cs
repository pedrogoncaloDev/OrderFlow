using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Web.Features.Products;

/// <summary>
/// Formulário de produto (cadastro e edição). As regras espelham as da API (ProductRequest); a API
/// continua sendo a fonte da verdade e valida tudo de novo.
/// </summary>
public sealed class ProductModel
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 1_000_000_000, ErrorMessage = "O preço deve estar entre 0 e 1.000.000.000.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo.")]
    public int StockQuantity { get; set; }

    public static ProductModel From(ProductInfo product) => new()
    {
        Name = product.Name,
        Description = product.Description ?? string.Empty,
        Price = product.Price,
        StockQuantity = product.StockQuantity
    };
}

/// <summary>Produto devolvido pela API.</summary>
public sealed record ProductInfo(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt);
