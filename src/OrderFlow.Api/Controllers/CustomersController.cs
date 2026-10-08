using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderFlow.Api.Extensions;
using OrderFlow.Api.Models.Common;
using OrderFlow.Api.Models.Customers;
using OrderFlow.Api.Requests;
using OrderFlow.Application.Auth;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Api.Controllers;

/// <summary>
/// Cadastro de clientes. Todo acesso é restrito ao dono do token (ADR 0004): cliente de outro
/// usuário responde 404, exatamente como se não existisse.
/// </summary>
[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _db;

    // Construtor
    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista os clientes do usuário, por nome, com busca (nome ou e-mail) e paginação.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<CustomerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        string? search,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (User.GetUserId() is not { } ownerId)
            return Unauthorized();

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _db.Customers.AsNoTracking().Where(c => c.OwnerId == ownerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(term) || c.Email.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<CustomerResponse>(items.ConvertAll(CustomerResponse.From), page, pageSize, total));
    }

    /// <summary>Retorna um cliente do usuário.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } ownerId)
            return Unauthorized();

        var customer = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == ownerId, cancellationToken);

        return customer is null ? NotFound() : Ok(CustomerResponse.From(customer));
    }

    /// <summary>Cadastra um cliente para o usuário.</summary>
    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CustomerRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } ownerId)
            return Unauthorized();

        var email = EmailNormalizer.Normalize(request.Email);

        if (await EmailInUseAsync(ownerId, email, exceptId: null, cancellationToken))
            return EmailConflict();

        var customer = new Customer
        {
            OwnerId = ownerId, // o dono vem sempre do token, nunca do corpo da requisição
            Name = request.Name.Trim(),
            Email = email,
            Phone = NormalizePhone(request.Phone)
        };

        _db.Customers.Add(customer);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Duas requisições com o mesmo e-mail chegaram juntas: o índice único (dono, e-mail) decide.
            return EmailConflict();
        }

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, CustomerResponse.From(customer));
    }

    /// <summary>Atualiza um cliente do usuário.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, CustomerRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } ownerId)
            return Unauthorized();

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == ownerId, cancellationToken);
        if (customer is null)
            return NotFound();

        var email = EmailNormalizer.Normalize(request.Email);

        if (await EmailInUseAsync(ownerId, email, exceptId: id, cancellationToken))
            return EmailConflict();

        customer.Name = request.Name.Trim();
        customer.Email = email;
        customer.Phone = NormalizePhone(request.Phone);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return EmailConflict();
        }

        return Ok(CustomerResponse.From(customer));
    }

    /// <summary>Exclui um cliente do usuário. Cliente com pedidos não pode ser excluído.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } ownerId)
            return Unauthorized();

        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.OwnerId == ownerId, cancellationToken);
        if (customer is null)
            return NotFound();

        if (await _db.Orders.AnyAsync(o => o.CustomerId == id, cancellationToken))
            return CustomerHasOrders();

        _db.Customers.Remove(customer);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // Um pedido deste cliente surgiu entre a checagem e a exclusão: a FK (Restrict) barrou.
            return CustomerHasOrders();
        }

        return NoContent();
    }

    private Task<bool> EmailInUseAsync(Guid ownerId, string email, Guid? exceptId, CancellationToken cancellationToken) =>
        _db.Customers.AnyAsync(c => c.OwnerId == ownerId && c.Email == email && c.Id != exceptId, cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private ObjectResult EmailConflict() => Problem(
        title: "E-mail já cadastrado.",
        detail: "Você já tem um cliente com este e-mail.",
        statusCode: StatusCodes.Status409Conflict);

    private ObjectResult CustomerHasOrders() => Problem(
        title: "Cliente com pedidos.",
        detail: "Este cliente tem pedidos e não pode ser excluído.",
        statusCode: StatusCodes.Status409Conflict);

    private static string? NormalizePhone(string? phone) =>
        string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
}
