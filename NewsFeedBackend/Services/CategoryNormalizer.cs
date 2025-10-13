using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;

namespace NewsFeedBackend.Services;

public sealed class CategoryNormalizer
{
    private readonly IChatCompletionService _chat;
    private readonly IPromptLoader _prompts;

    public CategoryNormalizer(IChatCompletionService chat, IPromptLoader prompts)
    {
        _chat = chat;
        _prompts = prompts;
    }

    public async Task<string?> NormalizeAsync(string? freeform, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(freeform)) return null;

        var system = _prompts.Load("CategoryMap");
        var user = JsonSerializer.Serialize(new { category = freeform });

        var history = new ChatHistory();
        history.AddSystemMessage(system);
        history.AddUserMessage(user);

        var reply = await _chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        var text = (reply.Content ?? string.Empty).Replace("```json", "").Replace("```", "").Trim();

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        if (root.TryGetProperty("Category", out var c) && c.ValueKind == JsonValueKind.String)
        {
            var val = c.GetString();
            return string.IsNullOrWhiteSpace(val) ? null : val;
        }
        if (root.TryGetProperty("Category", out var n) && n.ValueKind == JsonValueKind.Null) return null;

        return null;
    }
}
