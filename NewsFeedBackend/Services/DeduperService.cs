using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;

namespace NewsFeedBackend.Services;

public sealed record DeduperArticle(string Url, string Title, string Source);
public sealed record DeduperGroup(string CanonicalUrl, string[] Members, string Reason);
public sealed record DeduperResult(DeduperGroup[] Groups);

public sealed class DeduperService
{
    private readonly IChatCompletionService _chat;
    private readonly IPromptLoader _prompts;

    public DeduperService(IChatCompletionService chat, IPromptLoader prompts)
    {
        _chat = chat;
        _prompts = prompts;
    }

    public async Task<DeduperResult> DeduplicateAsync(IReadOnlyList<DeduperArticle> articles, CancellationToken ct = default)
    {
        if (articles.Count == 0) return new DeduperResult(Array.Empty<DeduperGroup>());

        var system = _prompts.Load("Deduplicate");
        var user = JsonSerializer.Serialize(new { articles });

        var history = new ChatHistory();
        history.AddSystemMessage(system);
        history.AddUserMessage(user);

        var reply = await _chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        var text = (reply.Content ?? string.Empty).Replace("```json", "").Replace("```", "").Trim();

        var result = JsonSerializer.Deserialize<DeduperResult>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return result ?? new DeduperResult(Array.Empty<DeduperGroup>());
    }
}
