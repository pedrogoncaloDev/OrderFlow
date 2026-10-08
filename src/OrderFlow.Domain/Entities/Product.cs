namespace OrderFlow.Domain.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Usuário dono do cadastro: cada usuário só enxerga os próprios produtos (ADR 0004).</summary>
    public Guid OwnerId { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
