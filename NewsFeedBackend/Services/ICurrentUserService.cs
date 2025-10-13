using System.Security.Claims;

namespace NewsFeedBackend.Services;

public interface ICurrentUserService
{
    Guid GetUserId(ClaimsPrincipal user);
}

public class CurrentUserService : ICurrentUserService
{
    public Guid GetUserId(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(id, out var userId))
            throw new InvalidOperationException("Bad user id in token.");
        return userId;
    }
}
