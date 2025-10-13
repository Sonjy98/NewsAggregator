using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Data;
using NewsFeedBackend.Errors;
using System.Text;

namespace NewsFeedBackend.Services;

public sealed record ProxyResult(int StatusCode, string Body);

public interface IExternalNewsService
{
    Task<ProxyResult> RawAsync(string? q, string? language, string? country, string? category, string? timeWindow, CancellationToken ct);
    Task<ProxyResult> SearchAsync(string q, string language, string? timeWindow, CancellationToken ct);
    Task<ProxyResult> ForUserAsync(ClaimsPrincipal user, string? language, string? category, string? timeWindow, CancellationToken ct);
}

public sealed class ExternalNewsService : IExternalNewsService
{
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly string _apiKey;

    private readonly string _defaultLanguage;
    private readonly string? _defaultQuery;
    private readonly string? _defaultCountry;
    private readonly string? _defaultCategory;

    public ExternalNewsService(
        IHttpClientFactory httpFactory,
        IConfiguration cfg,
        AppDbContext db,
        ICurrentUserService currentUser)
    {
        _http = httpFactory.CreateClient("newsdata");
        _db = db;
        _currentUser = currentUser;

        _apiKey = cfg["NewsData:ApiKey"] ?? throw new InvalidOperationException("Missing NewsData:ApiKey");
        _defaultLanguage = cfg["NewsData:DefaultLanguage"] ?? "en";
        _defaultQuery    = cfg["NewsData:DefaultQuery"];
        _defaultCountry  = cfg["NewsData:DefaultCountry"];
        _defaultCategory = cfg["NewsData:DefaultCategory"];
    }

    public Task<ProxyResult> RawAsync(string? q, string? language, string? country, string? category, string? timeWindow, CancellationToken ct)
    {
        var finalQ        = string.IsNullOrWhiteSpace(q) ? _defaultQuery : q;
        var finalLanguage = language ?? _defaultLanguage;
        var finalCountry  = country  ?? _defaultCountry;
        var finalCategory = category ?? _defaultCategory;
        var fromDate      = FromDateForWindow(timeWindow);

        var url = BuildUrl(finalQ, finalLanguage, finalCountry, finalCategory, fromDate);
        return ProxyAsync(url, ct);
    }

    public Task<ProxyResult> SearchAsync(string q, string language, string? timeWindow, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            throw new ValidationException("q required", code: "news/q-required");

        var fromDate = FromDateForWindow(timeWindow);
        var url = BuildUrl(q, language, country: null, category: null, fromDate);
        return ProxyAsync(url, ct);
    }

    public async Task<ProxyResult> ForUserAsync(ClaimsPrincipal user, string? language, string? category, string? timeWindow, CancellationToken ct)
    {
        var userId = _currentUser.GetUserId(user);

        var keywords = await _db.UserPreferences
            .Where(p => p.UserId == userId)
            .Select(p => p.Keyword)
            .ToListAsync(ct);

        var finalCategory = string.IsNullOrWhiteSpace(category) ? _defaultCategory : category;
        var lang = language ?? _defaultLanguage;
        var fromDate = FromDateForWindow(timeWindow);

        if (keywords.Count == 0)
        {
            var urlDefault = BuildUrl(_defaultQuery, lang, _defaultCountry, finalCategory, fromDate);
            return await ProxyAsync(urlDefault, ct);
        }

        var query = string.Join(" OR ", keywords.Select(k => $"\"{k}\""));
        var url = BuildUrl(query, lang, country: null, finalCategory, fromDate);
        return await ProxyAsync(url, ct);
    }

    // -------- internals --------
    static string? FromDateForWindow(string? timeWindow)
    {
        if (string.IsNullOrWhiteSpace(timeWindow)) return null;
        var now = DateTime.UtcNow;
        return timeWindow.Trim().ToLowerInvariant() switch
        {
            "24h" => now.AddDays(-1).ToString("yyyy-MM-dd"),
            "7d"  => now.AddDays(-7).ToString("yyyy-MM-dd"),
            "30d" => now.AddDays(-30).ToString("yyyy-MM-dd"),
            _     => null
        };
    }

    string BuildUrl(string? q, string? language, string? country, string? category, string? fromDate = null)
    {
        var sb = new StringBuilder($"news?apikey={_apiKey}");
        var lang = string.IsNullOrWhiteSpace(language) ? _defaultLanguage : language;

        sb.Append("&language=").Append(Uri.EscapeDataString(lang));
        if (!string.IsNullOrWhiteSpace(country))   sb.Append("&country=").Append(Uri.EscapeDataString(country!));
        if (!string.IsNullOrWhiteSpace(category))  sb.Append("&category=").Append(Uri.EscapeDataString(category!));
        if (!string.IsNullOrWhiteSpace(q))         sb.Append("&q=").Append(Uri.EscapeDataString(q!));
        if (!string.IsNullOrWhiteSpace(fromDate))  sb.Append("&from_date=").Append(Uri.EscapeDataString(fromDate!));

        return sb.ToString();
    }

    async Task<ProxyResult> ProxyAsync(string url, CancellationToken ct)
    {
        var resp = await _http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new ExternalServiceException($"News API error {(int)resp.StatusCode}: {body}", code: "news/upstream-error");

        return new ProxyResult((int)resp.StatusCode, body);
    }
}
