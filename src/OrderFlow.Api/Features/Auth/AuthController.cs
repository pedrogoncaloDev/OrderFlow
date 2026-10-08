using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderFlow.Application.Auth;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Auth;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IdentityPasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _tokenGenerator;

    // O hash da senha nunca sai daqui: UserResponse só expõe os dados públicos.
    private AuthResponse BuildAuthResponse(User user)
    {
        var (token, expiresAtUtc) = _tokenGenerator.Generate(user);

        return new AuthResponse(token, expiresAtUtc, UserResponse.From(user));
    }

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
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Email = EmailNormalizer.Normalize(request.Email),
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
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = EmailNormalizer.Normalize(request.Email);
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
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirst(AuthClaimTypes.Subject)?.Value;

        if (!Guid.TryParse(subject, out var userId))
            return Unauthorized();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? Unauthorized() : Ok(UserResponse.From(user));
    }

    /// <summary>Muda a senha do usuário.</summary>
    [HttpPost("change_password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        // Quem é o dono do token (já validado pelo middleware).
        if (!Guid.TryParse(User.FindFirst(AuthClaimTypes.Subject)?.Value, out var userId))
            return Unauthorized();

        // Tracking ligado: vamos alterar e salvar.
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return Unauthorized();

        // Confere a senha atual.
        if (!_passwordHasher.Verify(user.PasswordHash, request.old_password))
        {
            ModelState.AddModelError(nameof(request.old_password), "A senha atual está incorreta.");
            return ValidationProblem(ModelState);
        }

        if (request.old_password == request.new_password)
        {
            ModelState.AddModelError(nameof(request.new_password), "A nova senha deve ser diferente da atual.");
            return ValidationProblem(ModelState);
        }

        user.PasswordHash = _passwordHasher.Hash(request.new_password);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
