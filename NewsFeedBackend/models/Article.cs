namespace NewsFeedBackend.Models;
public record Article
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public string Author { get; init; } = "Unknown";
    public required string PublishedAt { get; init; }
    public string? Image { get; init; }
    public List<string> Categories { get; init; } = new();
    public string? RawDescription { get; init; }
    public string? Link { get; init; }
}
