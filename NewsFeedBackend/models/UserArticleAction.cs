namespace NewsFeedBackend.Models;

public enum ArticleActionType : byte { Viewed=0, Saved=1, Liked=2, Disliked=3, Dismissed=4 }

public sealed class UserArticleAction
{
    public Guid UserId { get; set; }
    public string ArticleId { get; set; } = default!;
    public ArticleActionType Action { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
