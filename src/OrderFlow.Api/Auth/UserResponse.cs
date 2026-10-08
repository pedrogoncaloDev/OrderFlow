using OrderFlow.Domain.Entities;

namespace OrderFlow.Api.Auth;

/// <summary>Dados públicos do usuário. O hash da senha nunca sai da API.</summary>
public sealed record UserResponse(Guid Id, string Email, string Role, DateTime CreatedAt)
{
    public static UserResponse From(User user) =>
        new(user.Id, user.Email, user.Role.ToString(), user.CreatedAt);
}
