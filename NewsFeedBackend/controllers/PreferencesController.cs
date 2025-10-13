using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Data;
using NewsFeedBackend.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PreferencesController(AppDbContext db, NewsFilterExtractor extractor) : ControllerBase
{
    private static readonly Regex SplitJoiners = new(@"\s*(?:,|&|/|\+|\band\b)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static IEnumerable<string> ExpandCompoundKeywords(IEnumerable<string?> inputs)
    {
        foreach (var raw in inputs)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var s = raw.Trim().Trim('\"', '\'', '“', '”');
            var parts = SplitJoiners.IsMatch(s)
                ? SplitJoiners.Split(s)
                : new[] { s };

            foreach (var p in parts)
            {
                var cleaned = (p ?? "").Trim().ToLowerInvariant();
                if (cleaned.Length is >= 1 and <= 128)
                    yield return cleaned;
            }
        }
    }

    private Guid GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(id, out var userId)) throw new InvalidOperationException("Bad user id in token.");
        return userId;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<string>>> List(CancellationToken ct)
        => Ok(await db.UserPreferences
            .Where(p => p.UserId == GetUserId())
            .OrderBy(p => p.Keyword)
            .Select(p => p.Keyword)
            .ToListAsync(ct));

    public record AddKeywordRequest(string Keyword);

    [HttpPost]
    public async Task<ActionResult<IEnumerable<string>>> Add(AddKeywordRequest req, CancellationToken ct)
    {
        var pieces = ExpandCompoundKeywords(new[] { req.Keyword }).ToArray();
        if (pieces.Length == 0) return BadRequest("Keyword length must be 1–128.");

        var uid = GetUserId();
        await SaveIncludeKeywordsAsync(uid, pieces, ct);
        return await List(ct);
    }

    [HttpDelete("{keyword}")]
    public async Task<IActionResult> Remove(string keyword, CancellationToken ct)
    {
        var kw = (keyword ?? "").Trim().ToLowerInvariant();
        var uid = GetUserId();
        var row = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == uid && p.Keyword == kw, ct);
        if (row is null) return NoContent();
        db.UserPreferences.Remove(row);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    public record NLPreferenceRequest(string Query);

    [HttpPost("natural-language")]
    public async Task<IActionResult> FromNaturalLanguage([FromBody] NLPreferenceRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Query))
            return BadRequest("Query is required.");

        var spec = await extractor.ExtractAsync(req.Query, ct);

        var uid = GetUserId();
        var toSave = ExpandCompoundKeywords(spec.IncludeKeywords ?? Array.Empty<string>()).ToArray();
        var added = await SaveIncludeKeywordsAsync(uid, toSave, ct);
        var total = await db.UserPreferences.CountAsync(p => p.UserId == uid, ct);

        return Ok(new
        {
            spec,
            saved = added,
            total
        });
    }

    private async Task<string[]> SaveIncludeKeywordsAsync(Guid uid, IEnumerable<string> candidates, CancellationToken ct)
    {
        var normalized = candidates
            .Select(s => (s ?? "").Trim().ToLowerInvariant())
            .Where(s => s.Length is >= 1 and <= 128)
            .Distinct()
            .ToArray();

        if (normalized.Length == 0) return Array.Empty<string>();

        var existing = await db.UserPreferences
            .Where(p => p.UserId == uid)
            .Select(p => p.Keyword)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existing, StringComparer.Ordinal);
        var capacity = 20 - existing.Count;
        if (capacity <= 0) return Array.Empty<string>();

        var toAdd = normalized.Where(s => !existingSet.Contains(s))
                              .Take(capacity)
                              .ToList();

        if (toAdd.Count > 0)
        {
            foreach (var kw in toAdd)
                db.UserPreferences.Add(new Models.UserPreference { UserId = uid, Keyword = kw });

            await db.SaveChangesAsync(ct);
        }

        return toAdd.ToArray();
    }
}
