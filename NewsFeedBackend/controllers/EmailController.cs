using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Data;
using NewsFeedBackend;
using System.Security.Claims;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailController(AppDbContext db, IHttpClientFactory httpFactory, IConfiguration cfg, IEmailSender mail)
    : ControllerBase
{
    private readonly AppDbContext _db = db;
    private readonly HttpClient _http = httpFactory.CreateClient("newsdata");
    private readonly string _apiKey = cfg["NewsData:ApiKey"] ?? throw new("Missing NewsData:ApiKey");
    private readonly string _defLang = cfg["NewsData:DefaultLanguage"] ?? "en";
    private readonly string? _defQuery = cfg["NewsData:DefaultQuery"];
    private readonly string? _defCountry = cfg["NewsData:DefaultCountry"];
    private readonly string? _defCategory= cfg["NewsData:DefaultCategory"];

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromQuery] int max = 10, [FromQuery] string? language = null, CancellationToken ct = default)
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(idStr, out var userId)) return Unauthorized("Bad user id.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return Unauthorized("User not found.");

        var keywords = await _db.UserPreferences.Where(p => p.UserId == userId).Select(p => p.Keyword).ToListAsync(ct);
        var q = keywords.Count == 0 ? _defQuery : string.Join(" OR ", keywords.Select(k => $"\"{k}\""));

        var url = $"news?apikey={_apiKey}&language={Uri.EscapeDataString(language ?? _defLang)}";
        if (!string.IsNullOrWhiteSpace(_defCountry))  url += $"&country={Uri.EscapeDataString(_defCountry!)}";
        if (!string.IsNullOrWhiteSpace(_defCategory)) url += $"&category={Uri.EscapeDataString(_defCategory!)}";
        if (!string.IsNullOrWhiteSpace(q)) url += $"&q={Uri.EscapeDataString(q)}";

        var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode, await resp.Content.ReadAsStringAsync(ct));

        var payload = await resp.Content.ReadFromJsonAsync<NewsDto>(cancellationToken: ct) ?? new();
        var items = (payload.results ?? []).Take(Math.Clamp(max, 1, 50)).ToList();

        var html = EmailHtml(user.Email, keywords, items);
        await mail.SendHtmlAsync(user.Email, "Your news digest", html, ct);

        return Ok(new { sentTo = user.Email, count = items.Count });
    }

    static string H(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
    static string EmailHtml(string toEmail, List<string> keywords, List<Article> items)
    {
        var kw = keywords.Count > 0 ? string.Join(", ", keywords) : "default feed";
        var rows = string.Join("", items.Select(it => $@"
          <tr>
            <td style=""padding:16px 0;border-bottom:1px solid #e9eef5"">
              <a href=""{H(it.link)}"" style=""font-size:18px;color:#0b5fff;text-decoration:none;font-weight:600"">{H(it.title ?? "Untitled")}</a>
              <div style=""color:#6b7280;font-size:12px;margin-top:4px"">
                {H((it.creator?.FirstOrDefault()) ?? "Unknown")} • {H(it.pubDate ?? "")}
              </div>
              {(string.IsNullOrEmpty(it.image_url) ? "" : $@"<img src=""{H(it.image_url)}"" alt="""" style=""max-width:100%;border-radius:8px;margin:10px 0""/>")}
              <div style=""color:#111827;font-size:14px;margin-top:6px"">{H(it.description ?? "")}</div>
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

    public class NewsDto { public string? status { get; set; } public List<Article> results { get; set; } = []; }
    public class Article {
        public string? title { get; set; } public string? description { get; set; }
        public List<string>? creator { get; set; } public string? pubDate { get; set; }
        public string? image_url { get; set; } public string? link { get; set; }
    }
}
