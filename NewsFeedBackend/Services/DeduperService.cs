using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel.ChatCompletion;

namespace NewsFeedBackend.Services;

public sealed record DeduperArticle(string Url, string Title, string Source);
public sealed record DeduperGroup(string CanonicalUrl, string[] Members, string Reason);
public sealed record DeduperResult(DeduperGroup[] Groups);

public sealed class DeduperService
{
    private readonly IChatCompletionService _chat;
    private readonly IWebHostEnvironment _env;
    private readonly string _promptTemplate;
    private readonly PromptCfg _cfg;

    public DeduperService(IChatCompletionService chat, IWebHostEnvironment env)
    {
        _chat = chat;
        _env  = env;

        var promptsDir = Path.Combine(_env.ContentRootPath, "Prompts");
        var promptPath = Path.Combine(promptsDir, "Deduplicate.prompt.txt");
        var configPath = Path.Combine(promptsDir, "Deduplicate.config.json");

        if (!File.Exists(promptPath))
            throw new FileNotFoundException($"Prompt file not found: {promptPath}");

        _promptTemplate = File.ReadAllText(promptPath);
        _cfg = PromptCfg.LoadIfExists(configPath);
    }

    public async Task<DeduperResult> DeduplicateAsync(
        IReadOnlyList<DeduperArticle> articles,
        CancellationToken ct = default)
    {
        if (articles is null || articles.Count == 0)
            return new DeduperResult(Array.Empty<DeduperGroup>());

        var payload = articles.Select(a => new { url = a.Url, title = a.Title, source = a.Source });
        var articlesJson = JsonSerializer.Serialize(payload);

        var prompt = _promptTemplate.Replace("{{articlesJson}}", articlesJson, StringComparison.Ordinal);

        var exec = _cfg.ToExecutionSettings(defaultTemp: 0.1, defaultTopP: 0.9, defaultMaxTokens: 700);

        var reply = await _chat.GetChatMessageContentAsync(
            prompt,
            exec,
            kernel: null,
            cancellationToken: ct
        );

        var text = (reply.Content ?? string.Empty)
            .Replace("```json", string.Empty)
            .Replace("```", string.Empty)
            .Trim();

        DeduperResult? parsed = null;

        try
        {
            parsed = JsonSerializer.Deserialize<DeduperResult>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            try
            {
                var groups = JsonSerializer.Deserialize<DeduperGroup[]>(text, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? Array.Empty<DeduperGroup>();
                parsed = new DeduperResult(groups);
            }
            catch { /* swallow and return empty below */ }
        }

        return parsed ?? new DeduperResult(Array.Empty<DeduperGroup>());
    }
}
