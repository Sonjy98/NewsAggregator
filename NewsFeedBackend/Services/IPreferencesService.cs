using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Data;
using NewsFeedBackend.Errors;
using System.Text.RegularExpressions;

namespace NewsFeedBackend.Services;

public record AddKeywordRequest(string Keyword);
public record NLPreferenceRequest(string Query);

public interface IPreferencesService
{
    Task<IReadOnlyList<string>> ListAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<string>> AddAsync(Guid userId, AddKeywordRequest req, CancellationToken ct);
    Task RemoveAsync(Guid userId, string keyword, CancellationToken ct);
    Task<(object spec, string[] saved, int total)> FromNaturalLanguageAsync(Guid userId, NLPreferenceRequest req, CancellationToken ct);
}

public class PreferencesService : IPreferencesService
{
    private readonly AppDbContext _db;
    private readonly NewsFilterExtractor _extractor;

    private static readonly Regex SplitJoiners = new(@"\s*(?:,|&|/|\+|\band\b)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PreferencesService(AppDbContext db, NewsFilterExtractor extractor)
    {
        _db = db;
        _extractor = extractor;
    }

    public async Task<IReadOnlyList<string>> ListAsync(Guid userId, CancellationToken ct)
        => await _db.UserPreferences
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Keyword)
            .Select(p => p.Keyword)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> AddAsync(Guid userId, AddKeywordRequest req, CancellationToken ct)
    {
        var pieces = ExpandCompoundKeywords(new[] { req.Keyword }).ToArray();
        if (pieces.Length == 0)
            throw new ValidationException("Keyword length must be 1–128.", code: "prefs/keyword-length");

        await SaveIncludeKeywordsAsync(userId, pieces, ct);
        return await ListAsync(userId, ct);
    }

    public async Task RemoveAsync(Guid userId, string keyword, CancellationToken ct)
    {
        var kw = (keyword ?? "").Trim().ToLowerInvariant();
        var row = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId && p.Keyword == kw, ct);
        if (row is null) return;
        _db.UserPreferences.Remove(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(object spec, string[] saved, int total)> FromNaturalLanguageAsync(Guid userId, NLPreferenceRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Query))
            throw new ValidationException("Query is required.", code: "prefs/query-required");

        var spec = await _extractor.ExtractAsync(req.Query, ct);

        var toSave = ExpandCompoundKeywords(spec.IncludeKeywords ?? Array.Empty<string>()).ToArray();
        var saved = await SaveIncludeKeywordsAsync(userId, toSave, ct);
        var total = await _db.UserPreferences.CountAsync(p => p.UserId == userId, ct);

        return (spec, saved, total);
    }

    // -------- internals --------
    private static IEnumerable<string> ExpandCompoundKeywords(IEnumerable<string?> inputs)
    {
        foreach (var raw in inputs)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var s = raw.Trim().Trim('\"', '\'', '“', '”');
            var parts = SplitJoiners.IsMatch(s) ? SplitJoiners.Split(s) : new[] { s };

            foreach (var p in parts)
            {
                var cleaned = (p ?? "").Trim().ToLowerInvariant();
                if (cleaned.Length is >= 1 and <= 128)
                    yield return cleaned;
            }
        }
    }

    private async Task<string[]> SaveIncludeKeywordsAsync(Guid uid, IEnumerable<string> candidates, CancellationToken ct)
    {
        var normalized = candidates
            .Select(s => (s ?? "").Trim().ToLowerInvariant())
            .Where(s => s.Length is >= 1 and <= 128)
            .Distinct()
            .ToArray();

        if (normalized.Length == 0) return Array.Empty<string>();

        var existing = await _db.UserPreferences
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
                _db.UserPreferences.Add(new Models.UserPreference { UserId = uid, Keyword = kw });

            await _db.SaveChangesAsync(ct);
        }

        return toAdd.ToArray();
    }
}
