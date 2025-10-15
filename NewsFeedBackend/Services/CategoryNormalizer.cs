using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;


namespace NewsFeedBackend.Services;

public sealed class CategoryNormalizer
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _func;

    public CategoryNormalizer(Kernel kernel, IWebHostEnvironment env)
    {
        _kernel = kernel;

        var dir = Path.Combine(env.ContentRootPath, "Prompts", "CategoryNormalizer");
        var hbs = new HandlebarsPromptTemplateFactory();

#pragma warning disable SKEXP0120
        var plugin = _kernel.CreatePluginFromPromptDirectory(
            pluginDirectory: dir,
            jsonSerializerOptions: new JsonSerializerOptions(),
            pluginName: "CategoryNormalizer",
            promptTemplateFactory: hbs
        );
#pragma warning restore SKEXP0120

        _kernel.Plugins.Add(plugin);

        _func = plugin["Normalize"];
    }

    public async Task<string?> NormalizeAsync(string? freeform, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(freeform)) return null;

        var args = new KernelArguments { ["rawCategory"] = freeform };
        var res  = await _kernel.InvokeAsync(_func, args, ct);
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
