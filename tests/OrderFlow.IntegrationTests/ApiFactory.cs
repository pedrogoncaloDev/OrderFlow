using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.IntegrationTests;

/// <summary>
/// Sobe a API inteira em memória, trocando o PostgreSQL por SQLite em memória (sem Docker).
/// O modelo é o mesmo do AppDbContext; o que é específico do Postgres (migrations, códigos de erro) fica de fora.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    // A conexão precisa continuar aberta: ao fechar, o banco em memória some.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); // chave JWT temporária; sem .env nem Postgres

        builder.UseSetting("ConnectionStrings:Default", "Host=unused");
        builder.UseSetting("Database:AutoMigrate", "false");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            _connection.Open();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection).UseSnakeCaseNamingConvention());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();

        return host;
    }

    /// <summary>Cria uma conta nova (e-mail único) e devolve um HttpClient já autenticado com ela.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = $"{Guid.NewGuid():N}@teste.com",
            Password = "Senha1234"
        });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthPayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }

    private sealed record AuthPayload(string AccessToken);
}
