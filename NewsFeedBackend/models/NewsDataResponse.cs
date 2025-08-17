namespace NewsFeedBackend.Models;

public record NewsDataResponse
{
    public string status { get; set; }
    public List<NewsItem> results { get; set; }
}

public record NewsItem
{
    public string article_id { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public List<string> creator { get; set; }
    public string pubDate { get; set; }
    public string image_url { get; set; }
    public List<string>? category { get; set; }
    public string? link { get; set; }
}
