// AI/PromptCfg.cs
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace NewsFeedBackend.Services;

public sealed class PromptCfg
{
    public double?   Temperature   { get; set; }
    public double?   TopP          { get; set; }
    public int?      MaxTokens     { get; set; }
    public string[]? StopSequences { get; set; }

    public static PromptCfg LoadIfExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new PromptCfg();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PromptCfg>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new PromptCfg();
    }

    public PromptExecutionSettings ToExecutionSettings(
        double defaultTemp = 0.2, double defaultTopP = 0.9, int defaultMaxTokens = 512)
    {
        var ext = new Dictionary<string, object>
        {
            ["temperature"]     = Temperature   ?? defaultTemp,
            ["topP"]            = TopP          ?? defaultTopP,
            ["maxOutputTokens"] = MaxTokens     ?? defaultMaxTokens
        };
        if (StopSequences is { Length: > 0 }) ext["stopSequences"] = StopSequences;

        return new PromptExecutionSettings { ExtensionData = ext };
    }
}
