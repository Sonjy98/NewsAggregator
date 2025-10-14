using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsFeedBackend.Data;
using NewsFeedBackend.Errors;
using NewsFeedBackend.Enums;
using NewsFeedBackend.Extensions;

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
    private const string ClientName = "newsdata";
    private const string CodeQRequired = "news/q-required";
    private const string CodeUpstreamError = "news/upstream-error";

    private readonly ILogger<ExternalNewsService> _logger;
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    private readonly string _apiKey;
    private readonly string _defaultLanguage;
    private readonly string? _defaultQuery;
    private readonly string? _defaultCountry;
    private readonly string? _defaultCategory;

    public ExternalNewsService(
        ILogger<ExternalNewsService> logger,
        IHttpClientFactory httpFactory,
        IConfiguration cfg,
        AppDbContext db,
        ICurrentUserService currentUser)
    {
        _logger = logger;
        _http = httpFactory.CreateClient(ClientName);
        _db = db;
        _currentUser = currentUser;

        _apiKey          = cfg["NewsData:ApiKey"]          ?? throw new InvalidOperationException("Missing NewsData:ApiKey");
        _defaultLanguage = cfg["NewsData:DefaultLanguage"] ?? "en";
        _defaultQuery    = cfg["NewsData:DefaultQuery"];
        _defaultCountry  = cfg["NewsData:DefaultCountry"];
        _defaultCategory = cfg["NewsData:DefaultCategory"];
    }

    public Task<ProxyResult> RawAsync(string? q, string? language, string? country, string? category, string? timeWindow, CancellationToken ct)
    {
        _logger.LogInformation("News/Raw start q='{Q}' lang='{Lang}' country='{Country}' category='{Category}' window='{Win}'",
            q, language, country, category, timeWindow);

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
        _logger.LogInformation("News/Search start q='{Q}' lang='{Lang}' window='{Win}'", q, language, timeWindow);

        if (string.IsNullOrWhiteSpace(q))
            throw new ValidationException("q required", code: CodeQRequired);

        var fromDate = FromDateForWindow(timeWindow);
        var url = BuildUrl(q, language, country: null, category: null, fromDate);
        return ProxyAsync(url, ct);
    }

    public async Task<ProxyResult> ForUserAsync(ClaimsPrincipal user, string? language, string? category, string? timeWindow, CancellationToken ct)
    {
        var userId = _currentUser.GetUserId(user);
        _logger.LogInformation("News/ForUser start user={UserId} lang='{Lang}' category='{Category}' window='{Win}'",
            userId, language, category, timeWindow);

        var keywords = await _db.UserPreferences
            .Where(p => p.UserId == userId)
            .Select(p => p.Keyword)
            .ToListAsync(ct);

        var finalCategory = string.IsNullOrWhiteSpace(category) ? _defaultCategory : category;
        var lang = language ?? _defaultLanguage;
        var fromDate = FromDateForWindow(timeWindow);

        if (keywords.Count == 0)
        {
            _logger.LogInformation("News/ForUser user={UserId} no-keywords → default feed", userId);
            var urlDefault = BuildUrl(_defaultQuery, lang, _defaultCountry, finalCategory, fromDate);
            return await ProxyAsync(urlDefault, ct);
        }

        var query = string.Join(" OR ", keywords.Select(k => $"\"{k}\""));
        _logger.LogInformation("News/ForUser user={UserId} kwCount={Count}", userId, keywords.Count);

        var url = BuildUrl(query, lang, country: null, finalCategory, fromDate);
        return await ProxyAsync(url, ct);
    }

    static string? FromDateForWindow(string? timeWindow)
    {
        if (!TimeWindowExtensions.TryParse(timeWindow, out var w)) return null;
        return w.ToFromDate();
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
        try
        {
            var resp = await _http.GetAsync(url, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("News/Proxy upstream error status={Status} len={Len}", (int)resp.StatusCode, body?.Length ?? 0);
                throw new ExternalServiceException($"News API error {(int)resp.StatusCode}: {TrimForLog(body)}", code: CodeUpstreamError);
            }

            _logger.LogInformation("News/Proxy ok status={Status} len={Len}", (int)resp.StatusCode, body?.Length ?? 0);
            return new ProxyResult((int)resp.StatusCode, body);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "News/Proxy timeout");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "News/Proxy transport error");
            throw;
        }
    }

    static string TrimForLog(string? s)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= 512 ? s : s[..512] + "…");
}
