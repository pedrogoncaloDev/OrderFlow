namespace OrderFlow.Api.Common;

/// <summary>Uma página de resultados, com o total para o cliente montar a paginação.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
