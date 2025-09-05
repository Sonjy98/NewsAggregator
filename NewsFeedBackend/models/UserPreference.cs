using System.ComponentModel.DataAnnotations;

namespace NewsFeedBackend.Models;

public class UserPreference
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(128)]
    public string Keyword { get; set; } = "";
}
