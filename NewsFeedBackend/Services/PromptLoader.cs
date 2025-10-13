using Microsoft.AspNetCore.Hosting;

namespace NewsFeedBackend.Services;

public interface IPromptLoader
{
    string Load(string name); // e.g., "NewsFilter"
}

public sealed class PromptLoader(IWebHostEnvironment env) : IPromptLoader
{
    public string Load(string name)
    {
        var path = Path.Combine(env.ContentRootPath, "Prompts", $"{name}.prompt.txt");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Prompt file not found: {path}");
        return File.ReadAllText(path);
    }
}
