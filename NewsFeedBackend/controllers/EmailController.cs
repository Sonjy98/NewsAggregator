using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsFeedBackend.Services;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IEmailDigestService _svc;

    public EmailController(IEmailDigestService svc) => _svc = svc;

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromQuery] int max = 10, [FromQuery] string? language = null, CancellationToken ct = default)
    {
        try
        {
            var result = await _svc.SendAsync(User, max, language, ct);
            return Ok(new { sentTo = result.SentTo, count = result.Count });
        }
        catch (UnauthorizedAccessException uex)
        {
            return Unauthorized(uex.Message);
        }
        catch (ArgumentException aex)
        {
            return BadRequest(aex.Message);
        }
        catch (InvalidOperationException iex)
        {
            // covers upstream News API errors, missing config, etc.
            return StatusCode(502, iex.Message);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}
