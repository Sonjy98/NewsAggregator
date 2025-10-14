using System.Security.Claims;
using Microsoft.Extensions.Logging;
using NewsFeedBackend.Errors;

namespace NewsFeedBackend.Services;

public interface ICurrentUserService
{
    Guid GetUserId(ClaimsPrincipal user);
    bool TryGetUserId(ClaimsPrincipal user, out Guid id);
}

public sealed class CurrentUserService : ICurrentUserService
{
    private static readonly string[] CandidateClaims =
    {
        ClaimTypes.NameIdentifier,
        "sub",
        "oid"
    };

    private const string CodeBadUserId = "auth/bad-userid";

    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(ILogger<CurrentUserService> logger) => _logger = logger;

    public Guid GetUserId(ClaimsPrincipal user)
    {
        if (TryGetUserId(user, out var id)) return id;

        var raw = GetRawId(user);
        _logger.LogWarning("GetUserId failed: rawId='{RawId}' claims={Claims}",
            raw ?? "<null>", string.Join(",", user?.Claims.Select(c => c.Type).Distinct() ?? Array.Empty<string>()));

        throw new UnauthorizedAppException("Unauthorized.", code: CodeBadUserId);
    }

    public bool TryGetUserId(ClaimsPrincipal user, out Guid id)
    {
        id = default;
        var raw = GetRawId(user);
        return Guid.TryParse(raw, out id);
    }

    private static string? GetRawId(ClaimsPrincipal user)
    {
        if (user is null) return null;
        foreach (var name in CandidateClaims)
        {
            var v = user.FindFirstValue(name);
            if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
        }
        return null;
    }
}
