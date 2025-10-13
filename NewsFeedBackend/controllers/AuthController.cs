using Microsoft.AspNetCore.Mvc;
using NewsFeedBackend.Models;
using NewsFeedBackend.Services;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        try
        {
            var res = await _auth.RegisterAsync(req, ct);
            return Ok(res);
        }
        catch (ArgumentException aex)
        {
            return BadRequest(aex.Message);
        }
        catch (InvalidOperationException iex) // email already registered
        {
            return Conflict(iex.Message);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        try
        {
            var res = await _auth.LoginAsync(req, ct);
            return Ok(res);
        }
        catch (ArgumentException aex)
        {
            return BadRequest(aex.Message);
        }
        catch (UnauthorizedAccessException uex)
        {
            return Unauthorized(uex.Message);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}
