using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Data;
using NewsFeedBackend.Services;
using System.Security.Claims;
using System.Linq;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PreferencesController(AppDbContext db, NewsFilterExtractor extractor) : ControllerBase
{
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
        var kw = (req.Keyword ?? "").Trim().ToLowerInvariant();
        if (kw.Length is < 1 or > 128) return BadRequest("Keyword length must be 1–128.");
        var uid = GetUserId();

        var exists = await db.UserPreferences.AnyAsync(p => p.UserId == uid && p.Keyword == kw, ct);
        if (!exists)
        {
            var count = await db.UserPreferences.CountAsync(p => p.UserId == uid, ct);
            if (count >= 20) return BadRequest("Max 20 keywords.");
            db.UserPreferences.Add(new Models.UserPreference { UserId = uid, Keyword = kw });
            await db.SaveChangesAsync(ct);
        }
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
        var added = await SaveIncludeKeywordsAsync(uid, spec.IncludeKeywords, ct);
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
