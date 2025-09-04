using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Data;
using System.Security.Claims;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")] // -> /api/preferences
[Authorize]
public class PreferencesController(AppDbContext db) : ControllerBase
{
    private Guid  GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid .TryParse(id, out var userId)) throw new InvalidOperationException("Bad user id in token.");
        return userId;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<string>>> List(CancellationToken ct)
        => Ok(await db.UserPreferences.Where(p => p.UserId == GetUserId()).OrderBy(p => p.Keyword).Select(p => p.Keyword).ToListAsync(ct));

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
}
