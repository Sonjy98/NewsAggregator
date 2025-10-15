namespace NewsFeedBackend.Models;

public sealed class UserSetting
{
    public Guid UserId { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PreferredCountry  { get; set; }
    public string? DefaultCategory   { get; set; }
    public string? DefaultTimeWindow { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
