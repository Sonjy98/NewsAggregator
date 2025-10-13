using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsFeedBackend.Errors;
using NewsFeedBackend.Models;
using NewsFeedBackend.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;
    public AuthController(IAuthService auth, ILogger<AuthController> logger) { _auth = auth; _logger = logger; }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _auth.RegisterAsync(req, ct));
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "Register failed");
            var problem = new ProblemDetails { Title = ex.GetType().Name, Detail = ex.Message, Status = ex.StatusCode };
            if (!string.IsNullOrWhiteSpace(ex.Code)) problem.Extensions["code"] = ex.Code;
            return StatusCode(ex.StatusCode, problem);
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _auth.LoginAsync(req, ct));
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "Login failed");
            var problem = new ProblemDetails { Title = ex.GetType().Name, Detail = ex.Message, Status = ex.StatusCode };
            if (!string.IsNullOrWhiteSpace(ex.Code)) problem.Extensions["code"] = ex.Code;
            return StatusCode(ex.StatusCode, problem);
        }
    }
}
