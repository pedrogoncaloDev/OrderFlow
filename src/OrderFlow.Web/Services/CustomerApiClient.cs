using OrderFlow.Web.Models;

namespace OrderFlow.Web.Services;

/// <summary>Cliente HTTP dos endpoints de clientes (/api/customers).</summary>
public sealed class CustomerApiClient : AuthorizedApiClient
{
    // Construtor
    public CustomerApiClient(HttpClient http, JwtAuthenticationStateProvider auth) : base(http, auth)
    {
    }

    public Task<ApiResult<PagedResult<CustomerInfo>>> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var url = $"api/customers?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
            url += "&search=" + Uri.EscapeDataString(search.Trim());

        return GetAsync<PagedResult<CustomerInfo>>(url, cancellationToken);
    }

    public Task<ApiResult<CustomerInfo>> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<CustomerInfo>($"api/customers/{id}", cancellationToken);

    public Task<ApiResult<CustomerInfo>> CreateAsync(CustomerModel model, CancellationToken cancellationToken = default) =>
        PostAsync<CustomerInfo>("api/customers", ToBody(model), cancellationToken);

    public Task<ApiResult<CustomerInfo>> UpdateAsync(Guid id, CustomerModel model, CancellationToken cancellationToken = default) =>
        PutAsync<CustomerInfo>($"api/customers/{id}", ToBody(model), cancellationToken);

    public Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/customers/{id}", cancellationToken);

    private static object ToBody(CustomerModel model) =>
        new { model.Name, model.Email, model.Phone };
}
