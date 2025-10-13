using System.Security.Claims;
using NewsFeedBackend.Errors;

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
            throw new UnauthorizedAppException("Bad user id in token.", code: "auth/bad-userid");
        return userId;
    }
}
