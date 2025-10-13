using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsFeedBackend.Services;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalNewsController : ControllerBase
{
    private readonly IExternalNewsService _svc;

    public ExternalNewsController(IExternalNewsService svc)
    {
        _svc = svc;
    }

    [HttpGet("newsdata")]
    [AllowAnonymous]
    public async Task<IActionResult> Raw(
        [FromQuery] string? q,
        [FromQuery] string? language,
        [FromQuery] string? country,
        [FromQuery] string? category,
        [FromQuery] string? timeWindow,
        CancellationToken ct = default)
    {
        try
        {
            var r = await _svc.RawAsync(q, language, country, category, timeWindow, ct);
            return StatusCode(r.StatusCode, r.Body);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] string language = "en",
        [FromQuery] string? timeWindow = null,
        CancellationToken ct = default)
    {
        try
        {
            var r = await _svc.SearchAsync(q, language, timeWindow, ct);
            return StatusCode(r.StatusCode, r.Body);
        }
        catch (ArgumentException aex)
        {
            return BadRequest(aex.Message);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpGet("for-me")]
    [Authorize]
    public async Task<IActionResult> ForMe(
        [FromQuery] string? language = null,
        [FromQuery] string? category = null,
        [FromQuery] string? timeWindow = null,
        CancellationToken ct = default)
    {
        try
        {
            var r = await _svc.ForUserAsync(User, language, category, timeWindow, ct);
            return StatusCode(r.StatusCode, r.Body);
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
