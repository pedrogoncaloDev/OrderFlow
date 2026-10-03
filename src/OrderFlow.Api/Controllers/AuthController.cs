using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Auth;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    // Construtor
    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Cria uma conta de cliente e já devolve o token de acesso.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.RegisterAsync(request, cancellationToken);

        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created, result.Response)
            : ToProblem(result.Error);
    }

    /// <summary>Autentica com e-mail e senha e devolve o token de acesso.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(request, cancellationToken);

        return result.Succeeded
            ? Ok(result.Response)
            : ToProblem(result.Error);
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

        var user = await _auth.GetUserAsync(userId, cancellationToken);

        return user is null ? Unauthorized() : Ok(user);
    }

    private ObjectResult ToProblem(AuthErrorCode? error) => error switch
    {
        AuthErrorCode.EmailAlreadyRegistered => Problem(
            title: "E-mail já cadastrado.",
            detail: "Já existe uma conta com este e-mail.",
            statusCode: StatusCodes.Status409Conflict),

        AuthErrorCode.InvalidCredentials => Problem(
            title: "Credenciais inválidas.",
            detail: "E-mail ou senha incorretos.",
            statusCode: StatusCodes.Status401Unauthorized),

        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
}
