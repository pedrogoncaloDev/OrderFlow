using System.Security.Claims;
using OrderFlow.Application.Auth;

namespace OrderFlow.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id do usuário dono do token (claim "sub"), ou nulo se a claim não existir ou for inválida.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst(AuthClaimTypes.Subject)?.Value, out var id) ? id : null;
}
