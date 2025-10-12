using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.ChatCompletion;

namespace NewsFeedBackend.Services;

public sealed record NewsFilterSpec(
    string[] IncludeKeywords,
    string[]? ExcludeKeywords,
    string[]? PreferredSources,
    string? Category,
    string? TimeWindow,
    string[]? MustHavePhrases,
    string[]? AvoidTopics
);

public sealed class NewsFilterExtractor
{
    private readonly IChatCompletionService _chat;

    public NewsFilterExtractor(IChatCompletionService chat) => _chat = chat;

    public async Task<NewsFilterSpec> ExtractAsync(string userQuery, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            throw new ArgumentException("Query cannot be empty.", nameof(userQuery));

        var system = """
            You convert a user's free-text news preference into STRICT JSON ONLY.
            Return a single minified JSON object with keys:
            - IncludeKeywords: string[]
            - ExcludeKeywords: string[] (optional)
            - PreferredSources: string[] (optional; e.g., "The Verge","BBC","Ars Technica")
            - Category: string (optional; one of: technology, business, science, world, sports, entertainment, health)
            - TimeWindow: string (optional; one of: 24h, 7d, 30d)
            - MustHavePhrases: string[] (optional; exact phrases)
            - AvoidTopics: string[] (optional)
            Rules:
            - No commentary, no Markdown, no code fences. JSON only.
            - Keep arrays short (<= 10 items).
            - Respect negatives like "no crypto" -> ExcludeKeywords or AvoidTopics.
            - If unsure, leave fields null/empty rather than guessing wildly.
            """;

        var user = $"User request: {userQuery}";

        var history = new ChatHistory();
        history.AddSystemMessage(system);
        history.AddUserMessage(user);

        var reply = await _chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        var text = (reply.Content ?? string.Empty).Trim();

        var json = ExtractFirstJsonObject(text)
                   ?? throw new InvalidOperationException("Model did not return JSON.");

        var spec = JsonSerializer.Deserialize<NewsFilterSpec>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        if (spec is null || spec.IncludeKeywords is null)
            throw new InvalidOperationException("Invalid JSON from model (IncludeKeywords missing).");

        spec = spec with
        {
            IncludeKeywords   = Normalize(spec.IncludeKeywords),
            ExcludeKeywords   = Normalize(spec.ExcludeKeywords),
            PreferredSources  = Normalize(spec.PreferredSources),
            MustHavePhrases   = Normalize(spec.MustHavePhrases),
            AvoidTopics       = Normalize(spec.AvoidTopics),
            Category          = NullIfEmpty(spec.Category),
            TimeWindow        = NormalizeWindow(spec.TimeWindow)
        };

        return spec;
    }

    static string[] Normalize(string[]? arr) =>
    arr is null
        ? Array.Empty<string>()
        : arr
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!) // assert non-null after filter
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();


    static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    static string? NormalizeWindow(string? w)
    {
        if (string.IsNullOrWhiteSpace(w)) return null;
        var t = w.Trim().ToLowerInvariant();
        return t switch { "24h" or "1d" => "24h", "7d" or "week" => "7d", "30d" or "month" => "30d", _ => null };
    }

    static string? ExtractFirstJsonObject(string text)
    {
        text = text.Replace("```json", "").Replace("```", "").Trim();

        var match = Regex.Match(text, @"\{(?:[^{}]|(?<o>\{)|(?<-o>\}))+(?(o)(?!))\}");
        return match.Success ? match.Value : null;
    }
}
