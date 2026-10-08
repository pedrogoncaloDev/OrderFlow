using System;

namespace OrderFlow.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
	public required string Name { get; set; } = string.Empty;
	public required string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
