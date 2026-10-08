namespace OrderFlow.Web.Services;

public static class ApiClientExtensions
{
    /// <summary>
    /// Registra (como scoped) toda classe concreta que herda de <see cref="ApiClientBase"/>, entregando a ela
    /// o HttpClient nomeado <see cref="ApiClientBase.HttpClientName"/>. Para criar um cliente novo basta herdar.
    /// </summary>
    public static IServiceCollection AddApiClients(this IServiceCollection services)
    {
        var clientTypes = typeof(ApiClientBase).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsClass: true } && type.IsSubclassOf(typeof(ApiClientBase)));

        foreach (var clientType in clientTypes)
        {
            services.AddScoped(clientType, provider =>
            {
                var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient(ApiClientBase.HttpClientName);

                return ActivatorUtilities.CreateInstance(provider, clientType, http);
            });
        }

        return services;
    }
}
