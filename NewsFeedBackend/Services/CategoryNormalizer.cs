using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel.ChatCompletion;

namespace NewsFeedBackend.Services;

public sealed class CategoryNormalizer
{
    private readonly IChatCompletionService _chat;
    private readonly IWebHostEnvironment _env;
    private readonly string _promptTemplate;
    private readonly PromptCfg _cfg;

    public CategoryNormalizer(IChatCompletionService chat, IWebHostEnvironment env)
    {
        _chat = chat;
        _env  = env;

        var promptsDir = Path.Combine(_env.ContentRootPath, "Prompts");
        var promptPath = Path.Combine(promptsDir, "CategoryMap.prompt.txt");
        var configPath = Path.Combine(promptsDir, "CategoryMap.config.json");

        if (!File.Exists(promptPath))
            throw new FileNotFoundException($"Prompt file not found: {promptPath}");

        _promptTemplate = File.ReadAllText(promptPath);
        _cfg = PromptCfg.LoadIfExists(configPath);
    }

    public async Task<string?> NormalizeAsync(string? freeform, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(freeform)) return null;

        var prompt = _promptTemplate.Replace("{{rawCategory}}", freeform, StringComparison.Ordinal);

        var exec = _cfg.ToExecutionSettings(defaultTemp: 0.0, defaultTopP: 0.9, defaultMaxTokens: 32);

        var reply = await _chat.GetChatMessageContentAsync(
            prompt,
            exec,
            kernel: null,
            cancellationToken: ct
        );

        var text = (reply.Content ?? string.Empty).Trim();

        text = text.Replace("```json", "").Replace("```", "").Trim();

        if (TryParseCategoryFromObject(text, out var catObj)) return catObj;

        if (TryParseJsonString(text, out var catStr)) return catStr;

        var fallback = text.Trim().Trim('"');
        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }

    private static bool TryParseCategoryFromObject(string json, out string? category)
    {
        category = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("category", out var cProp))
            {
                if (cProp.ValueKind == JsonValueKind.String)
                {
                    var v = cProp.GetString();
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        category = v!.Trim();
                        return true;
                    }
                }
                if (cProp.ValueKind == JsonValueKind.Null)
                {
                    category = null;
                    return true;
                }
            }
        }
        catch { /* ignore, try other shapes */ }
        return false;
    }

    private static bool TryParseJsonString(string json, out string? value)
    {
        value = null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                var v = doc.RootElement.GetString();
                if (!string.IsNullOrWhiteSpace(v))
                {
                    value = v!.Trim();
                    return true;
                }
                value = null;
                return true;
            }
        }
        catch { /* not a JSON string */ }
        return false;
    }
}
