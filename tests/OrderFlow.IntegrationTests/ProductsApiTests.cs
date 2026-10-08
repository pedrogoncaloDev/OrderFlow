using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.IntegrationTests;

public class ProductsApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    // Construtor
    public ProductsApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Requests_without_token_are_rejected()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_then_get_returns_the_saved_product()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var created = await CreateProductAsync(client, "  Camiseta  ", price: 39.9m, stock: 12, description: "  Algodão  ");

        Assert.Equal("Camiseta", created.Name);
        Assert.Equal("Algodão", created.Description);

        var fetched = await client.GetFromJsonAsync<ProductPayload>($"/api/products/{created.Id}");

        Assert.NotNull(fetched);
        Assert.Equal(39.9m, fetched.Price);
        Assert.Equal(12, fetched.StockQuantity);
    }

    [Theory]
    [InlineData("", 10, 1, "Name")]
    [InlineData("   ", 10, 1, "Name")]
    [InlineData("Produto", -1, 1, "Price")]
    [InlineData("Produto", 10.999, 1, "Price")]
    [InlineData("Produto", 10, -5, "StockQuantity")]
    public async Task Create_with_invalid_data_returns_validation_error(string name, decimal price, int stock, string field)
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/products", new { Name = name, Price = price, StockQuantity = stock });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationPayload>();
        Assert.Contains(field, problem!.Errors.Keys);
    }

    [Fact]
    public async Task Update_changes_the_product()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var created = await CreateProductAsync(client, "Boné", price: 59.9m, stock: 3);

        var response = await client.PutAsJsonAsync($"/api/products/{created.Id}",
            new { Name = "Boné aba reta", Description = (string?)null, Price = 64.5m, StockQuantity = 8 });

        response.EnsureSuccessStatusCode();

        var updated = await client.GetFromJsonAsync<ProductPayload>($"/api/products/{created.Id}");
        Assert.Equal("Boné aba reta", updated!.Name);
        Assert.Null(updated.Description);
        Assert.Equal(64.5m, updated.Price);
        Assert.Equal(8, updated.StockQuantity);
    }

    [Fact]
    public async Task Delete_removes_the_product()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var created = await CreateProductAsync(client, "Descartável");

        var delete = await client.DeleteAsync($"/api/products/{created.Id}");
        var get = await client.GetAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_of_a_sold_product_returns_conflict()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var created = await CreateProductAsync(client, "Vendido");

        await SellAsync(client, created.Id);

        var delete = await client.DeleteAsync($"/api/products/{created.Id}");
        var get = await client.GetAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task List_is_paged_sorted_by_name_and_filtered_by_search()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        foreach (var name in new[] { "Cadeira", "Mesa grande", "Abajur", "Mesa pequena" })
            await CreateProductAsync(client, name);

        var page1 = await client.GetFromJsonAsync<PagePayload>("/api/products?page=1&pageSize=3");
        var page2 = await client.GetFromJsonAsync<PagePayload>("/api/products?page=2&pageSize=3");
        var search = await client.GetFromJsonAsync<PagePayload>("/api/products?search=MESA");

        Assert.Equal(4, page1!.TotalCount);
        Assert.Equal(["Abajur", "Cadeira", "Mesa grande"], page1.Items.Select(p => p.Name));
        Assert.Equal(["Mesa pequena"], page2!.Items.Select(p => p.Name));
        Assert.Equal(2, search!.TotalCount);
    }

    [Fact]
    public async Task A_user_cannot_see_change_or_delete_products_of_another_user()
    {
        using var alice = await _factory.CreateAuthenticatedClientAsync();
        using var bob = await _factory.CreateAuthenticatedClientAsync();
        var aliceProduct = await CreateProductAsync(alice, "Produto da Alice");

        var get = await bob.GetAsync($"/api/products/{aliceProduct.Id}");
        var put = await bob.PutAsJsonAsync($"/api/products/{aliceProduct.Id}",
            new { Name = "Invadido", Price = 1, StockQuantity = 1 });
        var delete = await bob.DeleteAsync($"/api/products/{aliceProduct.Id}");
        var bobList = await bob.GetFromJsonAsync<PagePayload>("/api/products");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Empty(bobList!.Items);

        // O produto da Alice continua intacto.
        var stillThere = await alice.GetFromJsonAsync<ProductPayload>($"/api/products/{aliceProduct.Id}");
        Assert.Equal("Produto da Alice", stillThere!.Name);
    }

    private static async Task<ProductPayload> CreateProductAsync(
        HttpClient client, string name, decimal price = 10m, int stock = 5, string? description = null)
    {
        var response = await client.PostAsJsonAsync("/api/products",
            new { Name = name, Description = description, Price = price, StockQuantity = stock });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<ProductPayload>())!;
    }

    // Ainda não há endpoint de pedidos: grava direto no banco um pedido que contém o produto.
    private async Task SellAsync(HttpClient client, Guid productId)
    {
        var me = await client.GetFromJsonAsync<UserPayload>("/api/auth/me");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var customer = new Customer { Name = "Comprador", Email = $"{Guid.NewGuid():N}@teste.com" };
        var customerEntry = db.Add(customer);

        // Quando o cadastro de clientes tem dono (ADR 0004), o cliente pertence ao mesmo usuário.
        if (customerEntry.Metadata.FindProperty("OwnerId") is not null)
            customerEntry.Property("OwnerId").CurrentValue = me!.Id;

        var order = new Order { CustomerId = customer.Id, Status = Domain.Enums.OrderStatus.Confirmed, TotalAmount = 10m };
        order.Items.Add(new OrderItem { ProductId = productId, Quantity = 1, UnitPrice = 10m });
        db.Add(order);

        await db.SaveChangesAsync();
    }

    private sealed record UserPayload(Guid Id);

    private sealed record ProductPayload(Guid Id, string Name, string? Description, decimal Price, int StockQuantity);

    private sealed record PagePayload(List<ProductPayload> Items, int Page, int PageSize, int TotalCount);

    private sealed record ValidationPayload(Dictionary<string, string[]> Errors);
}
