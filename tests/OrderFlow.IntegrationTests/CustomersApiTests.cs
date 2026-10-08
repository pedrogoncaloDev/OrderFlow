using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.IntegrationTests;

public class CustomersApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    // Construtor
    public CustomersApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Requests_without_token_are_rejected()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_normalizes_the_data_and_get_returns_it()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var created = await CreateCustomerAsync(client, "  Maria Souza  ", "  Maria@Exemplo.com ", "  (11) 99999-0000 ");

        Assert.Equal("Maria Souza", created.Name);
        Assert.Equal("maria@exemplo.com", created.Email);
        Assert.Equal("(11) 99999-0000", created.Phone);

        var fetched = await client.GetFromJsonAsync<CustomerPayload>($"/api/customers/{created.Id}");
        Assert.Equal(created, fetched);
    }

    [Theory]
    [InlineData("", "a@b.com", "Name")]
    [InlineData("   ", "a@b.com", "Name")]
    [InlineData("Maria", "", "Email")]
    [InlineData("Maria", "isto-nao-e-email", "Email")]
    public async Task Create_with_invalid_data_returns_validation_error(string name, string email, string field)
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/customers", new { Name = name, Email = email });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationPayload>();
        Assert.Contains(field, problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_with_a_duplicate_email_for_the_same_owner_returns_conflict()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        await CreateCustomerAsync(client, "Maria", "maria@exemplo.com");

        var response = await client.PostAsJsonAsync("/api/customers", new { Name = "Outra Maria", Email = "MARIA@exemplo.com" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Two_users_can_register_the_same_customer_email()
    {
        using var alice = await _factory.CreateAuthenticatedClientAsync();
        using var bob = await _factory.CreateAuthenticatedClientAsync();
        await CreateCustomerAsync(alice, "Maria", "maria@exemplo.com");

        var response = await bob.PostAsJsonAsync("/api/customers", new { Name = "Maria", Email = "maria@exemplo.com" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Update_changes_the_customer_and_blocks_an_email_already_used_by_another()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var maria = await CreateCustomerAsync(client, "Maria", "maria@exemplo.com");
        await CreateCustomerAsync(client, "João", "joao@exemplo.com");

        var ok = await client.PutAsJsonAsync($"/api/customers/{maria.Id}",
            new { Name = "Maria Souza", Email = "maria@exemplo.com", Phone = (string?)null }); // mantém o próprio e-mail
        var conflict = await client.PutAsJsonAsync($"/api/customers/{maria.Id}",
            new { Name = "Maria Souza", Email = "joao@exemplo.com" });

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

        var updated = await client.GetFromJsonAsync<CustomerPayload>($"/api/customers/{maria.Id}");
        Assert.Equal("Maria Souza", updated!.Name);
        Assert.Equal("maria@exemplo.com", updated.Email);
    }

    [Fact]
    public async Task Delete_removes_the_customer()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var created = await CreateCustomerAsync(client, "Descartável", "descartavel@exemplo.com");

        var delete = await client.DeleteAsync($"/api/customers/{created.Id}");
        var get = await client.GetAsync($"/api/customers/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_of_a_customer_with_orders_returns_conflict()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        var created = await CreateCustomerAsync(client, "Com pedido", "compedido@exemplo.com");

        await PlaceOrderAsync(client, created.Id);

        var delete = await client.DeleteAsync($"/api/customers/{created.Id}");
        var get = await client.GetAsync($"/api/customers/{created.Id}");

        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task List_is_paged_sorted_by_name_and_searches_name_and_email()
    {
        using var client = await _factory.CreateAuthenticatedClientAsync();
        await CreateCustomerAsync(client, "Carla", "carla@exemplo.com");
        await CreateCustomerAsync(client, "Ana Lima", "ana@exemplo.com");
        await CreateCustomerAsync(client, "Bruno", "bruno.lima@exemplo.com");
        await CreateCustomerAsync(client, "Diego", "diego@exemplo.com");

        var page1 = await client.GetFromJsonAsync<PagePayload>("/api/customers?page=1&pageSize=3");
        var page2 = await client.GetFromJsonAsync<PagePayload>("/api/customers?page=2&pageSize=3");
        var byNameOrEmail = await client.GetFromJsonAsync<PagePayload>("/api/customers?search=LIMA");

        Assert.Equal(4, page1!.TotalCount);
        Assert.Equal(["Ana Lima", "Bruno", "Carla"], page1.Items.Select(c => c.Name));
        Assert.Equal(["Diego"], page2!.Items.Select(c => c.Name));
        Assert.Equal(["Ana Lima", "Bruno"], byNameOrEmail!.Items.Select(c => c.Name)); // nome de um, e-mail do outro
    }

    [Fact]
    public async Task A_user_cannot_see_change_or_delete_customers_of_another_user()
    {
        using var alice = await _factory.CreateAuthenticatedClientAsync();
        using var bob = await _factory.CreateAuthenticatedClientAsync();
        var aliceCustomer = await CreateCustomerAsync(alice, "Cliente da Alice", "cliente@exemplo.com");

        var get = await bob.GetAsync($"/api/customers/{aliceCustomer.Id}");
        var put = await bob.PutAsJsonAsync($"/api/customers/{aliceCustomer.Id}",
            new { Name = "Invadido", Email = "invadido@exemplo.com" });
        var delete = await bob.DeleteAsync($"/api/customers/{aliceCustomer.Id}");
        var bobList = await bob.GetFromJsonAsync<PagePayload>("/api/customers");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Empty(bobList!.Items);

        // O cliente da Alice continua intacto.
        var stillThere = await alice.GetFromJsonAsync<CustomerPayload>($"/api/customers/{aliceCustomer.Id}");
        Assert.Equal("Cliente da Alice", stillThere!.Name);
    }

    private static async Task<CustomerPayload> CreateCustomerAsync(
        HttpClient client, string name, string email, string? phone = null)
    {
        var response = await client.PostAsJsonAsync("/api/customers", new { Name = name, Email = email, Phone = phone });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return (await response.Content.ReadFromJsonAsync<CustomerPayload>())!;
    }

    // Ainda não há endpoint de pedidos: grava direto no banco um pedido do cliente.
    private async Task PlaceOrderAsync(HttpClient client, Guid customerId)
    {
        var me = await client.GetFromJsonAsync<UserPayload>("/api/auth/me");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product { Name = "Produto do pedido", Price = 10m, StockQuantity = 1 };
        var productEntry = db.Add(product);

        // Quando o cadastro de produtos tem dono (ADR 0004), o produto pertence ao mesmo usuário.
        if (productEntry.Metadata.FindProperty("OwnerId") is not null)
            productEntry.Property("OwnerId").CurrentValue = me!.Id;

        var order = new Order { CustomerId = customerId, Status = Domain.Enums.OrderStatus.Confirmed, TotalAmount = 10m };
        order.Items.Add(new OrderItem { ProductId = product.Id, Quantity = 1, UnitPrice = 10m });
        db.Add(order);

        await db.SaveChangesAsync();
    }

    private sealed record UserPayload(Guid Id);

    private sealed record CustomerPayload(Guid Id, string Name, string Email, string? Phone);

    private sealed record PagePayload(List<CustomerPayload> Items, int Page, int PageSize, int TotalCount);

    private sealed record ValidationPayload(Dictionary<string, string[]> Errors);
}
