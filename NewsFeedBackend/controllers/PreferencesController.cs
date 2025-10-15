using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsFeedBackend.Services;
using NewsFeedBackend.Constants;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PreferencesController : ApiControllerBase
{
    private readonly IPreferencesService _service;
    private readonly ICurrentUserService _currentUser;

    public PreferencesController(
        ILogger<PreferencesController> logger,
        IPreferencesService service,
        ICurrentUserService currentUser)
        : base(logger)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public Task<IActionResult> List(CancellationToken ct)
        => Safe(Operations.PreferencesList, async () =>
        {
            var uid = _currentUser.GetUserId(User);
            var result = await _service.ListAsync(uid, ct);
            return Ok(result);
        });

    [HttpPost]
    public Task<IActionResult> Add([FromBody] AddKeywordRequest req, CancellationToken ct)
        => Safe(Operations.PreferencesAdd, async () =>
        {
            var uid = _currentUser.GetUserId(User);
            var result = await _service.AddAsync(uid, req, ct);
            return Ok(result);
        });

    [HttpDelete("{keyword}")]
    public Task<IActionResult> Remove(string keyword, CancellationToken ct)
        => Safe(Operations.PreferencesRemove, async () =>
        {
            var uid = _currentUser.GetUserId(User);
            await _service.RemoveAsync(uid, keyword, ct);
            return NoContent();
        });

    [HttpPost("natural-language")]
    public Task<IActionResult> FromNaturalLanguage([FromBody] NLPreferenceRequest req, CancellationToken ct)
        => Safe(Operations.PreferencesNaturalLanguage, async () =>
        {
            var uid = _currentUser.GetUserId(User);
            var (spec, saved, total) = await _service.FromNaturalLanguageAsync(uid, req, ct);
            return Ok(new { spec, saved, total });
        });
}
