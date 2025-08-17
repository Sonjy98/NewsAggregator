using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Data;
using System.Security.Claims;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")] // -> /api/externalnews/...
public class ExternalNewsController : ControllerBase
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly AppDbContext _db;

    public ExternalNewsController(IHttpClientFactory httpFactory, IConfiguration cfg, AppDbContext db)
    {
        _http = httpFactory.CreateClient("newsdata");
        _apiKey = cfg["NewsData:ApiKey"] ?? throw new InvalidOperationException("Missing NewsData:ApiKey");
        _db = db;
    }

    // Fallback you already used before
    [HttpGet("newsdata")]
    [AllowAnonymous]
    public async Task<IActionResult> Raw(CancellationToken ct)
    {
        var resp = await _http.GetAsync($"news?apikey={_apiKey}&q=tech&language=en", ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        return StatusCode((int)resp.StatusCode, body);
    }

    // Keyword search
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string language = "en", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("q required");
        var url = $"news?apikey={_apiKey}&q={Uri.EscapeDataString(q)}&language={Uri.EscapeDataString(language)}";
        var resp = await _http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        return StatusCode((int)resp.StatusCode, body);
    }

    // For the logged-in user's preferences
    [HttpGet("for-me")]
    [Authorize]
    public async Task<IActionResult> ForMe([FromQuery] string language = "en", CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(userIdStr, out var userId)) return Unauthorized("Bad user id in token.");

        var keywords = await _db.UserPreferences.Where(p => p.UserId == userId).Select(p => p.Keyword).ToListAsync(ct);
        if (keywords.Count == 0) return Ok(new { results = Array.Empty<object>(), message = "No preferences set." });

        var query = string.Join(" OR ", keywords.Select(k => $"\"{k}\""));
        var url = $"news?apikey={_apiKey}&q={Uri.EscapeDataString(query)}&language={Uri.EscapeDataString(language)}";

        var resp = await _http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        return StatusCode((int)resp.StatusCode, body);
    }
}
