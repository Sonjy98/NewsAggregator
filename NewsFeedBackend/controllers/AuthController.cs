using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsFeedBackend.Controllers;
using NewsFeedBackend.Constants;
using NewsFeedBackend.Models;
using NewsFeedBackend.Services;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(ILogger<AuthController> logger, IAuthService auth)
        : base(logger) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
        => Safe(Operations.AuthRegister, async () =>
        {
            var result = await _auth.RegisterAsync(req, ct);
            return Ok(result);
        });

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
        => Safe(Operations.AuthLogin, async () =>
        {
            var result = await _auth.LoginAsync(req, ct);
            return Ok(result);
        });
}
