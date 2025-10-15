using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsFeedBackend.Constants;
using NewsFeedBackend.Services;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EmailController : ApiControllerBase
{
    private readonly IEmailDigestService _svc;
    public EmailController(ILogger<EmailController> logger, IEmailDigestService svc) : base(logger) => _svc = svc;

    [HttpPost("send")]
    public Task<IActionResult> Send([FromQuery] int max = 10, [FromQuery] string? language = null, CancellationToken ct = default)
        => Safe(Operations.EmailSend, async () =>
        {
            var result = await _svc.SendAsync(User, max, language, ct);
            return Ok(new { sentTo = result.SentTo, count = result.Count });
        });
}
