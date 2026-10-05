using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderFlow.Application.Auth;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Auth;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IdentityPasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _tokenGenerator;

    // Construtor
    public AuthController(AppDbContext db, IdentityPasswordHasher passwordHasher, JwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    /// <summary>Cria uma conta de cliente e já devolve o token de acesso.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Email = NormalizeEmail(request.Email),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Customer // o papel é sempre definido no servidor, nunca vem do cliente
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // O índice único de users.email é quem decide, inclusive quando duas requisições chegam juntas.
            return Problem(
                title: "E-mail já cadastrado.",
                detail: "Já existe uma conta com este e-mail.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return StatusCode(StatusCodes.Status201Created, BuildAuthResponse(user));
    }

    /// <summary>Autentica com e-mail e senha e devolve o token de acesso.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Mesma resposta para "usuário não existe" e "senha errada", para não revelar quais e-mails têm conta.
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return Problem(
                title: "Credenciais inválidas.",
                detail: "E-mail ou senha incorretos.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Ok(BuildAuthResponse(user));
    }

    /// <summary>Retorna os dados do usuário dono do token.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirst(AuthClaimTypes.Subject)?.Value;

        if (!Guid.TryParse(subject, out var userId))
            return Unauthorized();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? Unauthorized() : Ok(ToUserResponse(user));
    }

    // Resposta de cadastro e login: { accessToken, expiresAtUtc, user }. O hash da senha nunca sai daqui.
    private object BuildAuthResponse(User user)
    {
        var (token, expiresAtUtc) = _tokenGenerator.Generate(user);

        return new { AccessToken = token, ExpiresAtUtc = expiresAtUtc, User = ToUserResponse(user) };
    }

    private static object ToUserResponse(User user) =>
        new { user.Id, user.Email, Role = user.Role.ToString(), user.CreatedAt };

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

/// <summary>Corpo de POST /api/auth/register.</summary>
public sealed class RegisterRequest
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(320, ErrorMessage = "O e-mail deve ter no máximo 320 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(PasswordPolicy.MaxLength, MinimumLength = PasswordPolicy.MinLength,
        ErrorMessage = "A senha deve ter entre 8 e 100 caracteres.")]
    [RegularExpression(PasswordPolicy.Pattern, ErrorMessage = "A senha deve conter letras e números.")]
    public string Password { get; init; } = string.Empty;
}

/// <summary>Corpo de POST /api/auth/login.</summary>
public sealed class LoginRequest
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [StringLength(320, ErrorMessage = "O e-mail deve ter no máximo 320 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [StringLength(PasswordPolicy.MaxLength, ErrorMessage = "A senha deve ter no máximo 100 caracteres.")]
    public string Password { get; init; } = string.Empty;
}
