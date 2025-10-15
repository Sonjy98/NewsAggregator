using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

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
    private readonly KernelFunction _func;

    public NewsFilterExtractor(Kernel kernel, IWebHostEnvironment env)
    {
        _kernel = kernel;

        var dir = Path.Combine(env.ContentRootPath, "Prompts", "NewsFilter");
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Prompt directory not found: {dir}");

        _func = LoadFunctionFromPromptDir(dir);
    }

    public async Task<NewsFilterSpec> ExtractAsync(string userQuery, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            throw new ArgumentException("Query cannot be empty.", nameof(userQuery));

        var args = new KernelArguments { ["userQuery"] = userQuery };

        // Execution settings in config.json are applied automatically
        var result = await _kernel.InvokeAsync(_func, args, ct);
        var text   = (result.GetValue<string>() ?? string.Empty).Trim();

        var json = ExtractFirstJsonObject(text)
                   ?? throw new InvalidOperationException("Model did not return JSON.");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var include    = GetStringArray(root, "includeKeywords");
        var exclude    = GetStringArray(root, "excludeKeywords");
        var category   = GetString(root, "category");
        var timeWindow = NormalizeWindow(GetString(root, "timeWindow"));

        var spec = new NewsFilterSpec(
            IncludeKeywords  : Normalize(include),
            ExcludeKeywords  : Normalize(exclude),
            PreferredSources : null, // not used
            Category         : NullIfEmpty(category),
            TimeWindow       : timeWindow
        );

        if (spec.IncludeKeywords is null)
            throw new InvalidOperationException("Invalid JSON from model (includeKeywords missing).");

        return spec;
    }

    static KernelFunction LoadFunctionFromPromptDir(string dir)
    {
        var promptPath = Path.Combine(dir, "skprompt.txt");
        var configPath = Path.Combine(dir, "config.json");

        if (!File.Exists(promptPath))
            throw new FileNotFoundException($"Missing skprompt.txt in {dir}");
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Missing config.json in {dir}");

        var cfg = PromptTemplateConfig.FromJson(File.ReadAllText(configPath));

        cfg.TemplateFormat = "handlebars";
        cfg.Template = File.ReadAllText(promptPath);

        var hbs = new HandlebarsPromptTemplateFactory();

        return KernelFunctionFactory.CreateFromPrompt(cfg, hbs);
    }

    static string[] Normalize(string[]? arr) =>
        arr is null ? Array.Empty<string>() :
        arr.Select(s => s?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!)
           .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    static string? NormalizeWindow(string? w)
    {
        if (string.IsNullOrWhiteSpace(w)) return null;
        var t = w.Trim().ToLowerInvariant();
        return t switch
        {
            "24h" or "1d" or "day"     => "24h",
            "7d"  or "week" or "7days" => "7d",
            "30d" or "month"           => "30d",
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
        static string[] ReadArray(JsonElement e) =>
            e.ValueKind == JsonValueKind.Array
                ? e.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String)
                  .Select(x => x.GetString() ?? string.Empty).ToArray()
                : Array.Empty<string>();

        if (root.TryGetProperty(name, out var v)) return ReadArray(v);
        var pascal = char.ToUpperInvariant(name[0]) + name[1..];
        if (root.TryGetProperty(pascal, out v)) return ReadArray(v);
        return Array.Empty<string>();
    }
}
