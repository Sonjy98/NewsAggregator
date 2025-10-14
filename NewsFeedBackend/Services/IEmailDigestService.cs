using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsFeedBackend.Data;
using NewsFeedBackend.Errors;
using NewsFeedBackend.Models; // <= to use your domain Article

namespace NewsFeedBackend.Services;

public sealed record EmailDigestResult(string SentTo, int Count);

public interface IEmailDigestService
{
    Task<EmailDigestResult> SendAsync(ClaimsPrincipal user, int max, string? language, CancellationToken ct);
}

public sealed class EmailDigestService : IEmailDigestService
{
    private const string ClientName        = "newsdata";
    private const string CodeUserMissing   = "email/user-missing";
    private const string CodeUpstreamError = "email/upstream-error";
    private const int    MaxItemsClamp     = 50;
    private const string DigestSubject     = "Your news digest";

    private readonly ILogger<EmailDigestService> _logger;
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly IEmailSender _mail;
    private readonly ICurrentUserService _current;

    private readonly string _apiKey;
    private readonly string _defLang;
    private readonly string? _defQuery;
    private readonly string? _defCountry;
    private readonly string? _defCategory;

    public EmailDigestService(
        ILogger<EmailDigestService> logger,
        AppDbContext db,
        IHttpClientFactory httpFactory,
        IConfiguration cfg,
        IEmailSender mail,
        ICurrentUserService current)
    {
        _logger   = logger;
        _db       = db;
        _http     = httpFactory.CreateClient(ClientName);
        _mail     = mail;
        _current  = current;

        _apiKey      = cfg["NewsData:ApiKey"]          ?? throw new("Missing NewsData:ApiKey");
        _defLang     = cfg["NewsData:DefaultLanguage"] ?? "en";
        _defQuery    = cfg["NewsData:DefaultQuery"];
        _defCountry  = cfg["NewsData:DefaultCountry"];
        _defCategory = cfg["NewsData:DefaultCategory"];
    }

    public async Task<EmailDigestResult> SendAsync(ClaimsPrincipal userPrincipal, int max, string? language, CancellationToken ct)
    {
        var userId = _current.GetUserId(userPrincipal);
        _logger.LogInformation("EmailDigest/Send start user={UserId} max={Max} lang='{Lang}'", userId, max, language);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new UnauthorizedAppException("User not found.", code: CodeUserMissing);

        var keywords = await _db.UserPreferences
            .Where(p => p.UserId == userId)
            .Select(p => p.Keyword)
            .ToListAsync(ct);

        var q = keywords.Count == 0 ? _defQuery : string.Join(" OR ", keywords.Select(k => $"\"{k}\""));

        var url = $"news?apikey={_apiKey}&language={Uri.EscapeDataString(language ?? _defLang)}";
        if (!string.IsNullOrWhiteSpace(_defCountry))  url += $"&country={Uri.EscapeDataString(_defCountry!)}";
        if (!string.IsNullOrWhiteSpace(_defCategory)) url += $"&category={Uri.EscapeDataString(_defCategory!)}";
        if (!string.IsNullOrWhiteSpace(q))            url += $"&q={Uri.EscapeDataString(q)}";

        try
        {
            var resp = await _http.GetAsync(url, ct);
            var bodyLenHdr = resp.Content.Headers.ContentLength;

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("EmailDigest/upstream error status={Status} len={Len}", (int)resp.StatusCode, body?.Length ?? bodyLenHdr ?? 0);
                throw new ExternalServiceException($"News API error {(int)resp.StatusCode}: {TrimForLog(body)}", code: CodeUpstreamError);
            }

            var payload = await resp.Content.ReadFromJsonAsync<NewsDataResponseDto>(cancellationToken: ct)
                          ?? new NewsDataResponseDto();

            var rawItems = payload.results ?? new List<NewsDataArticleDto>();
            var items = rawItems
                .Take(Math.Clamp(max, 1, MaxItemsClamp))
                .Select((it, i) => MapToDomain(it, i))
                .ToList();

            var html = EmailHtml(user.Email, keywords, items);
            await _mail.SendHtmlAsync(user.Email, DigestSubject, html, ct);

            _logger.LogInformation("EmailDigest/sent user={UserId} to={Email} count={Count}", userId, user.Email, items.Count);
            return new EmailDigestResult(user.Email, items.Count);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "EmailDigest/timeout user={UserId}", userId);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "EmailDigest/http-error user={UserId}", userId);
            throw;
        }
    }

    private static Article MapToDomain(NewsDataArticleDto it, int index) => new Article
    {
        Id            = it.link ?? index.ToString(),
        Title         = it.title ?? "Untitled",
        Body          = it.description ?? string.Empty,
        Author        = it.creator?.FirstOrDefault() ?? "Unknown",
        PublishedAt   = it.pubDate ?? string.Empty,
        Image         = it.image_url,
        Categories    = new List<string>(),
        RawDescription = it.description,
        Link          = it.link
    };

    private static string H(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");

    private static string EmailHtml(string toEmail, List<string> keywords, List<Article> items)
    {
        var kw = keywords.Count > 0 ? string.Join(", ", keywords) : "default feed";
        var rows = string.Join("", items.Select(it => $@"
          <tr>
            <td style=""padding:16px 0;border-bottom:1px solid #e9eef5"">
              <a href=""{H(it.Link)}"" style=""font-size:18px;color:#0b5fff;text-decoration:none;font-weight:600"">{H(it.Title)}</a>
              <div style=""color:#6b7280;font-size:12px;margin-top:4px"">
                {H(it.Author)} • {H(it.PublishedAt)}
              </div>
              {(string.IsNullOrEmpty(it.Image) ? "" : $@"<img src=""{H(it.Image)}"" alt="""" style=""max-width:100%;border-radius:8px;margin:10px 0""/>")}
              <div style=""color:#111827;font-size:14px;margin-top:6px"">{H(it.Body)}</div>
            </td>
          </tr>"));
        return $@"<!doctype html><html><body style='margin:0;background:#f6f7fb;font-family:system-ui,-apple-system,Segoe UI,Roboto,Arial;color:#111827'>
        <table role='presentation' width='100%'><tr><td align='center'>
          <table role='presentation' width='640' style='max-width:640px;background:#fff;border:1px solid #e9eef5;border-radius:12px;padding:18px'>
            <tr><td>
              <h2 style='margin:0 0 8px;'>📰 Your news digest</h2>
              <div style='color:#6b7280;font-size:13px;margin-bottom:8px'>Keywords: {H(kw)}</div>
              <hr style='border:none;border-top:1px solid #e9eef5;margin:8px 0'/>
              <table role='presentation' width='100%'>{rows}</table>
              <div style='color:#9ca3af;font-size:12px;margin-top:12px'>Sent to {H(toEmail)}</div>
            </td></tr>
          </table>
        </td></tr></table></body></html>";
    }

    private static string TrimForLog(string? s)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= 512 ? s : s[..512] + "…");


    private sealed class NewsDataResponseDto
    {
        public string? status { get; set; }
        public List<NewsDataArticleDto> results { get; set; } = new List<NewsDataArticleDto>();
    }

    private sealed class NewsDataArticleDto
    {
        public string? title { get; set; }
        public string? description { get; set; }
        public List<string>? creator { get; set; }
        public string? pubDate { get; set; }
        public string? image_url { get; set; }
        public string? link { get; set; }
    }
}
