using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;

namespace NewsFeedBackend.Services;

public sealed record NewsFilterSpec(
    string[] IncludeKeywords,
    string[]? ExcludeKeywords,
    string[]? PreferredSources,
    string? Category,
    string? TimeWindow
);

public sealed class NewsFilterExtractor
{
    private readonly Kernel _kernel;
    private readonly IWebHostEnvironment _env;
    private readonly KernelFunction _func;

    public NewsFilterExtractor(Kernel kernel, IWebHostEnvironment env)
    {
        _kernel = kernel;
        _env = env;
        var promptPath = Path.Combine(_env.ContentRootPath, "Prompts", "NewsFilter.prompt.txt");
        var prompt = File.ReadAllText(promptPath);
        _func = KernelFunctionFactory.CreateFromPrompt(prompt);
    }

    public async Task<NewsFilterSpec> ExtractAsync(string userQuery, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            throw new ArgumentException("Query cannot be empty.", nameof(userQuery));

        var vars = new KernelArguments { ["userQuery"] = userQuery };
        var result = await _kernel.InvokeAsync(_func, vars, ct);
        var text = (result.GetValue<string>() ?? string.Empty).Trim();

        var json = ExtractFirstJsonObject(text)
                   ?? throw new InvalidOperationException("Model did not return JSON.");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var include          = GetStringArray(root, "includeKeywords");
        var exclude          = GetStringArray(root, "excludeKeywords");
        var preferredSources = GetStringArray(root, "preferredSources");
        var category         = GetString(root, "category");
        var timeWindow       = NormalizeWindow(GetString(root, "timeWindow"));

        var spec = new NewsFilterSpec(
            IncludeKeywords  : Normalize(include),
            ExcludeKeywords  : Normalize(exclude),
            PreferredSources : Normalize(preferredSources),
            Category         : NullIfEmpty(category),
            TimeWindow       : timeWindow
        );

        if (spec.IncludeKeywords is null)
            throw new InvalidOperationException("Invalid JSON from model (includeKeywords missing).");

        return spec;
    }

    static string[] Normalize(string[]? arr) =>
        arr is null ? Array.Empty<string>() :
        arr.Select(s => s?.Trim())
           .Where(s => !string.IsNullOrWhiteSpace(s))
           .Select(s => s!)
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .ToArray();

    static string? NullIfEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    static string? NormalizeWindow(string? w)
    {
        if (string.IsNullOrWhiteSpace(w)) return null;
        var t = w.Trim().ToLowerInvariant();
        return t switch
        {
            "24h" or "1d" or "day" => "24h",
            "7d" or "week" or "7days" => "7d",
            "30d" or "month" => "30d",
            _ => null
        };
    }

    static string? ExtractFirstJsonObject(string text)
    {
        text = text.Replace("```json", "").Replace("```", "").Trim();
        var match = Regex.Match(text, @"\{(?:[^{}]|(?<o>\{)|(?<-o>\}))+(?(o)(?!))\}");
        return match.Success ? match.Value : null;
    }

    static string? GetString(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
            return v.GetString();

        var pascal = char.ToUpperInvariant(name[0]) + name[1..];
        if (root.TryGetProperty(pascal, out v) && v.ValueKind == JsonValueKind.String)
            return v.GetString();

        return null;
    }

    static string[] GetStringArray(JsonElement root, string name)
    {
        static string[] ReadArray(JsonElement e)
            => e.ValueKind == JsonValueKind.Array
                ? e.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString() ?? string.Empty)
                    .ToArray()
                : Array.Empty<string>();

        if (root.TryGetProperty(name, out var v))
            return ReadArray(v);

        var pascal = char.ToUpperInvariant(name[0]) + name[1..];
        if (root.TryGetProperty(pascal, out v))
            return ReadArray(v);

        return Array.Empty<string>();
    }
}
