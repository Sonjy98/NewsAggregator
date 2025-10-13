using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsFeedBackend.Services;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ExternalNewsController : ApiControllerBase
{
    private readonly IExternalNewsService _svc;
    public ExternalNewsController(ILogger<ExternalNewsController> logger, IExternalNewsService svc) : base(logger) => _svc = svc;

    [HttpGet("newsdata")]
    [AllowAnonymous]
    public Task<IActionResult> Raw(string? q, string? language, string? country, string? category, string? timeWindow, CancellationToken ct = default)
        => Safe("ExternalNews/Raw", async () =>
        {
            var r = await _svc.RawAsync(q, language, country, category, timeWindow, ct);
            return StatusCode(r.StatusCode, r.Body);
        });

    [HttpGet("search")]
    [AllowAnonymous]
    public Task<IActionResult> Search(string q, string language = "en", string? timeWindow = null, CancellationToken ct = default)
        => Safe("ExternalNews/Search", async () =>
        {
            var r = await _svc.SearchAsync(q, language, timeWindow, ct);
            return StatusCode(r.StatusCode, r.Body);
        });

    [HttpGet("for-me")]
    [Authorize]
    public Task<IActionResult> ForMe(string? language = null, string? category = null, string? timeWindow = null, CancellationToken ct = default)
        => Safe("ExternalNews/ForMe", async () =>
        {
            var r = await _svc.ForUserAsync(User, language, category, timeWindow, ct);
            return StatusCode(r.StatusCode, r.Body);
        });
}
