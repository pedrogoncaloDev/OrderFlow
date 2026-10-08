using OrderFlow.Domain.Entities;

namespace OrderFlow.Api.Features.Products;

/// <summary>Dados do produto devolvidos pela API (sem o dono, que é sempre o usuário do token).</summary>
public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    DateTime CreatedAt)
{
    public static ProductResponse From(Product product) =>
        new(product.Id, product.Name, product.Description, product.Price, product.StockQuantity, product.CreatedAt);
}
