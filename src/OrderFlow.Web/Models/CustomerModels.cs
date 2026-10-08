using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Web.Models;

/// <summary>
/// Formulário de cliente (cadastro e edição). As regras espelham as da API (CustomerRequest); a API
/// continua sendo a fonte da verdade e valida tudo de novo.
/// </summary>
public sealed class CustomerModel
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(200, ErrorMessage = "O nome deve ter no máximo 200 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(320, ErrorMessage = "O e-mail deve ter no máximo 320 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "O telefone deve ter no máximo 30 caracteres.")]
    public string Phone { get; set; } = string.Empty;

    public static CustomerModel From(CustomerInfo customer) => new()
    {
        Name = customer.Name,
        Email = customer.Email,
        Phone = customer.Phone ?? string.Empty
    };
}

/// <summary>Cliente devolvido pela API.</summary>
public sealed record CustomerInfo(Guid Id, string Name, string Email, string? Phone, DateTime CreatedAt);
