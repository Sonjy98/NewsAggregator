using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private const int MinKeywordLen = 1;
    private const int MaxKeywordLen = 128;
    private const int MaxKeywordsPerUser = 20;

    private const string CodeKeywordLength = "prefs/keyword-length";
    private const string CodeQueryRequired = "prefs/query-required";

    private const string SplitJoinersPattern = @"\s*(?:,|&|/|\+|\band\b)\s*";
    private static readonly Regex SplitJoiners = new(SplitJoinersPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly NewsFilterExtractor _extractor;
    private readonly ILogger<PreferencesService> _logger;

    public PreferencesService(AppDbContext db, NewsFilterExtractor extractor, ILogger<PreferencesService> logger)
    {
        _db = db;
        _extractor = extractor;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> ListAsync(Guid userId, CancellationToken ct)
    {
        _logger.LogInformation("Prefs/List start UserId={UserId}", userId);
        try
        {
            var list = await _db.UserPreferences
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.Keyword)
                .Select(p => p.Keyword)
                .ToListAsync(ct);

            _logger.LogInformation("Prefs/List done UserId={UserId} Count={Count}", userId, list.Count);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prefs/List failed UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> AddAsync(Guid userId, AddKeywordRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Prefs/Add start UserId={UserId} Raw='{Raw}'", userId, req.Keyword);
        try
        {
            var pieces = ExpandCompoundKeywords(new[] { req.Keyword }).ToArray();
            if (pieces.Length == 0)
                throw new ValidationException($"Keyword length must be {MinKeywordLen}–{MaxKeywordLen}.", code: CodeKeywordLength);

            var saved = await SaveIncludeKeywordsAsync(userId, pieces, ct);
            _logger.LogInformation("Prefs/Add saved UserId={UserId} SavedCount={Count}", userId, saved.Length);

            var list = await ListAsync(userId, ct);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prefs/Add failed UserId={UserId}", userId);
            throw;
        }
    }

    public async Task RemoveAsync(Guid userId, string keyword, CancellationToken ct)
    {
        _logger.LogInformation("Prefs/Remove start UserId={UserId} Kw='{Keyword}'", userId, keyword);
        try
        {
            var kw = (keyword ?? "").Trim().ToLowerInvariant();
            var row = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId && p.Keyword == kw, ct);
            if (row is null)
            {
                _logger.LogInformation("Prefs/Remove noop UserId={UserId} Kw='{Keyword}' not found", userId, kw);
                return;
            }

            _db.UserPreferences.Remove(row);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Prefs/Remove done UserId={UserId} Kw='{Keyword}'", userId, kw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prefs/Remove failed UserId={UserId} Kw='{Keyword}'", userId, keyword);
            throw;
        }
    }

    public async Task<(object spec, string[] saved, int total)> FromNaturalLanguageAsync(Guid userId, NLPreferenceRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Prefs/NaturalLanguage start UserId={UserId}", userId);
        try
        {
            if (string.IsNullOrWhiteSpace(req.Query))
                throw new ValidationException("Query is required.", code: CodeQueryRequired);

            var spec = await _extractor.ExtractAsync(req.Query, ct);

            var toSave = ExpandCompoundKeywords(spec.IncludeKeywords ?? Array.Empty<string>()).ToArray();
            var saved = await SaveIncludeKeywordsAsync(userId, toSave, ct);
            var total = await _db.UserPreferences.CountAsync(p => p.UserId == userId, ct);

            _logger.LogInformation("Prefs/NaturalLanguage done UserId={UserId} Saved={Saved} Total={Total}",
                userId, saved.Length, total);

            return (spec, saved, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prefs/NaturalLanguage failed UserId={UserId}", userId);
            throw;
        }
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
                if (cleaned.Length is >= MinKeywordLen and <= MaxKeywordLen)
                    yield return cleaned;
            }
        }
    }

    private async Task<string[]> SaveIncludeKeywordsAsync(Guid uid, IEnumerable<string> candidates, CancellationToken ct)
    {
        var normalized = candidates
            .Select(s => (s ?? "").Trim().ToLowerInvariant())
            .Where(s => s.Length is >= MinKeywordLen and <= MaxKeywordLen)
            .Distinct()
            .ToArray();

        if (normalized.Length == 0) return Array.Empty<string>();

        var existing = await _db.UserPreferences
            .Where(p => p.UserId == uid)
            .Select(p => p.Keyword)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existing, StringComparer.Ordinal);
        var capacity = MaxKeywordsPerUser - existing.Count;
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