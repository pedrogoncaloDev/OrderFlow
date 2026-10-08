using Microsoft.AspNetCore.Components.Authorization;
using OrderFlow.Web.Components;
using OrderFlow.Web.Auth;
using OrderFlow.Web.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHealthChecks();

// Autenticação do front-end: o JWT emitido pela API fica guardado no navegador (criptografado)
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());

// Cliente HTTP da API
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Configure 'Api:BaseUrl' (variável de ambiente 'Api__BaseUrl') com o endereço da OrderFlow.Api.");

builder.Services.AddHttpClient(ApiClientBase.HttpClientName, client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Todo cliente da API (classe que herda de ApiClientBase) é registrado sozinho, com o HttpClient acima.
builder.Services.AddApiClients();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHealthChecks("/health");

app.Run();
