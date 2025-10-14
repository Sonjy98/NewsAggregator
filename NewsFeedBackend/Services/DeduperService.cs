using System.Text.Json;
using Microsoft.SemanticKernel;

namespace NewsFeedBackend.Services;

public sealed record DeduperArticle(string Url, string Title, string Source);
public sealed record DeduperGroup(string CanonicalUrl, string[] Members, string Reason);
public sealed record DeduperResult(DeduperGroup[] Groups);

public sealed class DeduperService
{
    private readonly Kernel _kernel;
    private readonly IWebHostEnvironment _env;
    private readonly KernelFunction _func;

    public DeduperService(Kernel kernel, IWebHostEnvironment env)
    {
        _kernel = kernel;
        _env = env;
        var promptPath = Path.Combine(_env.ContentRootPath, "Prompts", "Deduplicate.prompt.txt");
        var prompt = File.ReadAllText(promptPath);
        _func = KernelFunctionFactory.CreateFromPrompt(prompt);
    }

    public async Task<DeduperResult> DeduplicateAsync(IReadOnlyList<DeduperArticle> articles, CancellationToken ct = default)
    {
        if (articles.Count == 0) return new DeduperResult(Array.Empty<DeduperGroup>());

        // Project to lower-case field names to match the prompt wording
        var payload = articles.Select(a => new { url = a.Url, title = a.Title, source = a.Source }).ToList();
        var articlesJson = JsonSerializer.Serialize(payload);

        var vars = new KernelArguments { ["articlesJson"] = articlesJson };
        var res = await _kernel.InvokeAsync(_func, vars, ct);
        var text = (res.GetValue<string>() ?? string.Empty).Replace("```json", "").Replace("```", "").Trim();

        var result = JsonSerializer.Deserialize<DeduperResult>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result ?? new DeduperResult(Array.Empty<DeduperGroup>());
    }
}
