using OrderFlow.Domain.Entities;

namespace OrderFlow.Api.Models.Customers;

/// <summary>Dados do cliente devolvidos pela API (sem o dono, que é sempre o usuário do token).</summary>
public sealed record CustomerResponse(Guid Id, string Name, string Email, string? Phone, DateTime CreatedAt)
{
    public static CustomerResponse From(Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.Phone, customer.CreatedAt);
}
