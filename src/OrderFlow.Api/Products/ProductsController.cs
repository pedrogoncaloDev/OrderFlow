using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderFlow.Api.Common;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Api.Products;

/// <summary>
/// Cadastro de produtos. Todo acesso é restrito ao dono do token (ADR 0004): produto de outro
/// usuário responde 404, exatamente como se não existisse.
/// </summary>
[ApiController]
[Authorize]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _db;

    // Construtor
    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Lista os produtos do usuário, por nome, com busca opcional e paginação.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResponse<ProductResponse>>(StatusCodes.Status200OK)]
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

        var query = _db.Products.AsNoTracking().Where(p => p.OwnerId == ownerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<ProductResponse>(items.ConvertAll(ProductResponse.From), page, pageSize, total));
    }

    /// <summary>Retorna um produto do usuário.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } ownerId)
            return Unauthorized();

        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, cancellationToken);

        return product is null ? NotFound() : Ok(ProductResponse.From(product));
    }

    /// <summary>Cadastra um produto para o usuário.</summary>
    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } ownerId)
            return Unauthorized();

        var product = new Product
        {
            OwnerId = ownerId, // o dono vem sempre do token, nunca do corpo da requisição
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ProductResponse.From(product));
    }

    /// <summary>Atualiza um produto do usuário.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, ProductRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } ownerId)
            return Unauthorized();

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, cancellationToken);
        if (product is null)
            return NotFound();

        product.Name = request.Name.Trim();
        product.Description = NormalizeDescription(request.Description);
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ProductResponse.From(product));
    }

    /// <summary>Exclui um produto do usuário. Produto já vendido não pode ser excluído.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } ownerId)
            return Unauthorized();

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, cancellationToken);
        if (product is null)
            return NotFound();

        if (await _db.OrderItems.AnyAsync(i => i.ProductId == id, cancellationToken))
            return ProductSold();

        _db.Products.Remove(product);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // Um pedido com este produto surgiu entre a checagem e a exclusão: a FK (Restrict) barrou.
            return ProductSold();
        }

        return NoContent();
    }

    private ObjectResult ProductSold() => Problem(
        title: "Produto já vendido.",
        detail: "Este produto aparece em pedidos e não pode ser excluído.",
        statusCode: StatusCodes.Status409Conflict);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
