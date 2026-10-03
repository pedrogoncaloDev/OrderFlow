using OrderFlow.Application.Auth;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.UnitTests.Auth;

public class AuthServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly AuthService _sut;

    // Construtor: o xUnit cria uma instância nova da classe para cada teste
    public AuthServiceTests()
    {
        _sut = new AuthService(_users, new FakePasswordHasher(), new FakeTokenGenerator());
    }

    // ---------- Cadastro ----------

    [Fact]
    public async Task Register_WithNewEmail_CreatesCustomerAndReturnsToken()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest { Email = "maria@exemplo.com", Password = "senha1234" });

        Assert.True(result.Succeeded);
        Assert.Equal("fake-token", result.Response!.AccessToken);
        Assert.Equal("maria@exemplo.com", result.Response.User.Email);

        var saved = Assert.Single(_users.Users);
        Assert.Equal(UserRole.Customer, saved.Role);
    }

    [Fact]
    public async Task Register_NormalizesEmailToLowercaseWithoutSpaces()
    {
        await _sut.RegisterAsync(new RegisterRequest { Email = "  Maria@Exemplo.COM ", Password = "senha1234" });

        Assert.Equal("maria@exemplo.com", Assert.Single(_users.Users).Email);
    }

    [Fact]
    public async Task Register_StoresHashInsteadOfPlainPassword()
    {
        await _sut.RegisterAsync(new RegisterRequest { Email = "maria@exemplo.com", Password = "senha1234" });

        var saved = Assert.Single(_users.Users);
        Assert.NotEqual("senha1234", saved.PasswordHash);
        Assert.Equal("hash:senha1234", saved.PasswordHash);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsEmailAlreadyRegistered()
    {
        await _sut.RegisterAsync(new RegisterRequest { Email = "maria@exemplo.com", Password = "senha1234" });

        // Mesmo e-mail com maiúsculas: precisa ser tratado como repetido
        var result = await _sut.RegisterAsync(new RegisterRequest { Email = "MARIA@exemplo.com", Password = "outra5678" });

        Assert.False(result.Succeeded);
        Assert.Equal(AuthErrorCode.EmailAlreadyRegistered, result.Error);
        Assert.Single(_users.Users);
    }

    [Fact]
    public async Task Register_WhenRepositoryReportsDuplicate_ReturnsEmailAlreadyRegistered()
    {
        // Corrida: a checagem inicial passou, mas o índice único do banco recusou o INSERT
        _users.TryAddShouldFail = true;

        var result = await _sut.RegisterAsync(new RegisterRequest { Email = "maria@exemplo.com", Password = "senha1234" });

        Assert.False(result.Succeeded);
        Assert.Equal(AuthErrorCode.EmailAlreadyRegistered, result.Error);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        await _sut.RegisterAsync(new RegisterRequest { Email = "maria@exemplo.com", Password = "senha1234" });

        var result = await _sut.LoginAsync(new LoginRequest { Email = "maria@exemplo.com", Password = "senha1234" });

        Assert.True(result.Succeeded);
        Assert.Equal("fake-token", result.Response!.AccessToken);
        Assert.Equal(FakeTokenGenerator.ExpiresAt, result.Response.ExpiresAtUtc);
    }

    [Fact]
    public async Task Login_IgnoresEmailCaseAndSurroundingSpaces()
    {
        await _sut.RegisterAsync(new RegisterRequest { Email = "maria@exemplo.com", Password = "senha1234" });

        var result = await _sut.LoginAsync(new LoginRequest { Email = " MARIA@Exemplo.com ", Password = "senha1234" });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsInvalidCredentials()
    {
        await _sut.RegisterAsync(new RegisterRequest { Email = "maria@exemplo.com", Password = "senha1234" });

        var result = await _sut.LoginAsync(new LoginRequest { Email = "maria@exemplo.com", Password = "errada999" });

        Assert.False(result.Succeeded);
        Assert.Equal(AuthErrorCode.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsSameErrorAsWrongPassword()
    {
        var result = await _sut.LoginAsync(new LoginRequest { Email = "ninguem@exemplo.com", Password = "senha1234" });

        Assert.False(result.Succeeded);
        Assert.Equal(AuthErrorCode.InvalidCredentials, result.Error);
    }

    // ---------- Consulta do usuário ----------

    [Fact]
    public async Task GetUser_WithExistingId_ReturnsPublicDataWithoutHash()
    {
        var registered = await _sut.RegisterAsync(new RegisterRequest { Email = "maria@exemplo.com", Password = "senha1234" });

        var user = await _sut.GetUserAsync(registered.Response!.User.Id);

        Assert.NotNull(user);
        Assert.Equal("maria@exemplo.com", user.Email);
        Assert.Equal("Customer", user.Role);
    }

    [Fact]
    public async Task GetUser_WithUnknownId_ReturnsNull()
    {
        Assert.Null(await _sut.GetUserAsync(Guid.NewGuid()));
    }
}
