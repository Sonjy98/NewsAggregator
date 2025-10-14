using System.Text.Json;
using Microsoft.SemanticKernel;

namespace NewsFeedBackend.Services;

public sealed class CategoryNormalizer
{
    private readonly Kernel _kernel;
    private readonly IWebHostEnvironment _env;
    private readonly KernelFunction _func;

    public CategoryNormalizer(Kernel kernel, IWebHostEnvironment env)
    {
        _kernel = kernel;
        _env = env;
        var promptPath = Path.Combine(_env.ContentRootPath, "Prompts", "CategoryMap.prompt.txt");
        var prompt = File.ReadAllText(promptPath);
        _func = KernelFunctionFactory.CreateFromPrompt(prompt);
    }

    public async Task<string?> NormalizeAsync(string? freeform, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(freeform)) return null;

        var vars = new KernelArguments { ["rawCategory"] = freeform };
        var res = await _kernel.InvokeAsync(_func, vars, ct);
        var text = (res.GetValue<string>() ?? string.Empty).Trim();

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        if (root.TryGetProperty("category", out var c))
        {
            if (c.ValueKind == JsonValueKind.Null) return null;
            if (c.ValueKind == JsonValueKind.String)
            {
                var val = c.GetString();
                return string.IsNullOrWhiteSpace(val) ? null : val;
            }
        }
        return null;
    }
}
