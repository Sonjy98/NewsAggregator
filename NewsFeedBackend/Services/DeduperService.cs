using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;


namespace NewsFeedBackend.Services;

public sealed record DeduperArticle(string Url, string Title, string Source);
public sealed record DeduperGroup(string CanonicalUrl, string[] Members, string Reason);
public sealed record DeduperResult(DeduperGroup[] Groups);

public sealed class DeduperService
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _func;

    public DeduperService(Kernel kernel, IWebHostEnvironment env)
    {
        _kernel = kernel;

        var dir = Path.Combine(env.ContentRootPath, "Prompts", "Deduper");
        var hbs = new HandlebarsPromptTemplateFactory();

#pragma warning disable SKEXP0120
        var plugin = _kernel.CreatePluginFromPromptDirectory(
            pluginDirectory: dir,
            jsonSerializerOptions: new JsonSerializerOptions(),
            pluginName: "Deduper",
            promptTemplateFactory: hbs
        );
#pragma warning restore SKEXP0120

        _kernel.Plugins.Add(plugin);
        _func = plugin["Dedupe"];
    }

    public async Task<DeduperResult> DeduplicateAsync(IReadOnlyList<DeduperArticle> articles, CancellationToken ct = default)
    {
        if (articles.Count == 0) return new DeduperResult(Array.Empty<DeduperGroup>());

        var payload = articles.Select(a => new { url = a.Url, title = a.Title, source = a.Source }).ToList();
        var articlesJson = JsonSerializer.Serialize(payload);

        var args = new KernelArguments { ["articlesJson"] = articlesJson };

        // execution_settings from config.json are applied automatically
        var res = await _kernel.InvokeAsync(_func, args, ct);
        var text = (res.GetValue<string>() ?? string.Empty).Replace("```json", "").Replace("```", "").Trim();

        var result = JsonSerializer.Deserialize<DeduperResult>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result ?? new DeduperResult(Array.Empty<DeduperGroup>());
    }
}
