using System.Net;

namespace OrderFlow.Web.Common;

/// <summary>Resultado de uma chamada à API: ou o valor, ou uma mensagem de erro pronta para exibir.</summary>
public sealed record ApiResult<T>(bool Succeeded, T? Value, string? Error, HttpStatusCode? StatusCode)
{
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;

    public static ApiResult<T> Ok(T value) => new(true, value, null, null);

    public static ApiResult<T> Fail(string error, HttpStatusCode? statusCode = null) =>
        new(false, default, error, statusCode);
}
