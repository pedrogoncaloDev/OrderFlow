using OrderFlow.Web.Models;

namespace OrderFlow.Web.Services;

/// <summary>Cliente HTTP dos endpoints de produtos (/api/products).</summary>
public sealed class ProductApiClient : AuthorizedApiClient
{
    // Construtor
    public ProductApiClient(HttpClient http, JwtAuthenticationStateProvider auth) : base(http, auth)
    {
    }

    public Task<ApiResult<PagedResult<ProductInfo>>> ListAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var url = $"api/products?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrWhiteSpace(search))
            url += "&search=" + Uri.EscapeDataString(search.Trim());

        return GetAsync<PagedResult<ProductInfo>>(url, cancellationToken);
    }

    public Task<ApiResult<ProductInfo>> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        GetAsync<ProductInfo>($"api/products/{id}", cancellationToken);

    public Task<ApiResult<ProductInfo>> CreateAsync(ProductModel model, CancellationToken cancellationToken = default) =>
        PostAsync<ProductInfo>("api/products", ToBody(model), cancellationToken);

    public Task<ApiResult<ProductInfo>> UpdateAsync(Guid id, ProductModel model, CancellationToken cancellationToken = default) =>
        PutAsync<ProductInfo>($"api/products/{id}", ToBody(model), cancellationToken);

    public Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/products/{id}", cancellationToken);

    private static object ToBody(ProductModel model) =>
        new { model.Name, model.Description, model.Price, model.StockQuantity };
}
