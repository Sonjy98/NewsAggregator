namespace NewsFeedBackend.Models;

public sealed class ExternalArticle
{
    public string Id { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string? Title { get; set; }
    public string? Source { get; set; }
    public string? Language { get; set; }
    public string? Category { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime FetchedAt { get; set; }
    public string? Summary { get; set; }
    public double? Score { get; set; }
    public string? RawJson { get; set; }
    public string UrlHash { get; private set; } = null!;

}
