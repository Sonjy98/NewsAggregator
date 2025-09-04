using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Data;
using System.Security.Claims;
using System.Text;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalNewsController : ControllerBase
{
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly string _apiKey;

    // defaults (configurable)
    private readonly string _defaultLanguage;
    private readonly string? _defaultQuery;
    private readonly string? _defaultCountry;
    private readonly string? _defaultCategory;

    public ExternalNewsController(IHttpClientFactory httpFactory, IConfiguration cfg, AppDbContext db)
    {
        _http = httpFactory.CreateClient("newsdata");
        _db = db;
        _apiKey = cfg["NewsData:ApiKey"] ?? throw new InvalidOperationException("Missing NewsData:ApiKey");

    
        _defaultLanguage = cfg["NewsData:DefaultLanguage"] ?? "en";
        _defaultQuery    = cfg["NewsData:DefaultQuery"]; 
        _defaultCountry  = cfg["NewsData:DefaultCountry"];
        _defaultCategory = cfg["NewsData:DefaultCategory"];
    }

    private string BuildUrl(string? q, string? language, string? country, string? category)
    {
        var sb = new StringBuilder($"news?apikey={_apiKey}");
        var lang = string.IsNullOrWhiteSpace(language) ? _defaultLanguage : language;

        sb.Append("&language=").Append(Uri.EscapeDataString(lang));
        if (!string.IsNullOrWhiteSpace(country))  sb.Append("&country=").Append(Uri.EscapeDataString(country!));
        if (!string.IsNullOrWhiteSpace(category)) sb.Append("&category=").Append(Uri.EscapeDataString(category!));
        if (!string.IsNullOrWhiteSpace(q))        sb.Append("&q=").Append(Uri.EscapeDataString(q!));

        return sb.ToString();
    }

    private async Task<IActionResult> ProxyAsync(string url, CancellationToken ct)
    {
        var resp = await _http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        return StatusCode((int)resp.StatusCode, body);
    }

    // Public/default feed (uses defaults if none provided)
    // GET /api/externalnews/newsdata[?q=...&language=...&country=...&category=...]
    [HttpGet("newsdata")]
    [AllowAnonymous]
    public Task<IActionResult> Raw(
        [FromQuery] string? q,
        [FromQuery] string? language,
        [FromQuery] string? country,
        [FromQuery] string? category,
        CancellationToken ct = default)
    {
        // If q not provided, use configured default query (or none) + default language/country/category
        var finalQ        = string.IsNullOrWhiteSpace(q) ? _defaultQuery : q;
        var finalLanguage = language ?? _defaultLanguage;
        var finalCountry  = country  ?? _defaultCountry;
        var finalCategory = category ?? _defaultCategory;

        return ProxyAsync(BuildUrl(finalQ, finalLanguage, finalCountry, finalCategory), ct);
    }

    // GET /api/externalnews/search?q=...&language=...
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string language = "en", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("q required");
        return await ProxyAsync(BuildUrl(q, language, null, null), ct);
    }

    // GET /api/externalnews/for-me?language=en
    // If user has no keywords → fall back to default feed (not empty!)
    [HttpGet("for-me")]
    [Authorize]
    public async Task<IActionResult> ForMe([FromQuery] string? language = null, CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized("Bad user id in token.");

        var keywords = await _db.UserPreferences
            .Where(p => p.UserId == userId)
            .Select(p => p.Keyword)
            .ToListAsync(ct);

        if (keywords.Count == 0)
        {
            // server-side fallback to your default feed
            var urlDefault = BuildUrl(_defaultQuery, language ?? _defaultLanguage, _defaultCountry, _defaultCategory);
            return await ProxyAsync(urlDefault, ct);
        }

        var query = string.Join(" OR ", keywords.Select(k => $"\"{k}\""));
        var url = BuildUrl(query, language, null, null);
        return await ProxyAsync(url, ct);
    }
}
