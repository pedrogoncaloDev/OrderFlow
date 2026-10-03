using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Auth;

public sealed record GeneratedToken(string Value, DateTime ExpiresAtUtc);

public interface ITokenGenerator
{
    GeneratedToken Generate(User user);
}
