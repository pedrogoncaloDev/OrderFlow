using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Extensions;
using OrderFlow.Infrastructure.Persistence;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuth(builder.Configuration, builder.Environment);
builder.Services.AddControllers();
builder.Services.AddHealthChecks(); // só "o processo está de pé"; não consulta o banco (não acorda o Neon)
builder.Services.AddOpenApi();

var app = builder.Build();

await app.ApplyMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static class StartupExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? BuildConnectionStringFromEnv();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        return services;
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:AutoMigrate"))
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    private static string BuildConnectionStringFromEnv() =>
        $"Host=localhost;Port={RequireEnv("POSTGRES_PORT")};" +
        $"Database={RequireEnv("POSTGRES_DB")};" +
        $"Username={RequireEnv("POSTGRES_USER")};" +
        $"Password={RequireEnv("POSTGRES_PASSWORD")}";

    private static string RequireEnv(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException(
            $"Variável de ambiente '{name}' não definida. Crie o .env a partir do .env.example.");
}
