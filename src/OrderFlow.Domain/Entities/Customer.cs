namespace OrderFlow.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Usuário dono do cadastro: cada usuário só enxerga os próprios clientes (ADR 0004).</summary>
    public Guid OwnerId { get; set; }

    public required string Name { get; set; }

    /// <summary>E-mail na forma canônica (aparado e em minúsculas); único por dono.</summary>
    public required string Email { get; set; }

    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
