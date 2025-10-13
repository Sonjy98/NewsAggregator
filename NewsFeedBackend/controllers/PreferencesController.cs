using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsFeedBackend.Services;

namespace NewsFeedBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly IPreferencesService _service;
    private readonly ICurrentUserService _currentUser;

    public PreferencesController(IPreferencesService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        try
        {
            var uid = _currentUser.GetUserId(User);
            var result = await _service.ListAsync(uid, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddKeywordRequest req, CancellationToken ct)
    {
        try
        {
            var uid = _currentUser.GetUserId(User);
            var result = await _service.AddAsync(uid, req, ct);
            return Ok(result);
        }
        catch (ArgumentException aex)
        {
            return BadRequest(aex.Message);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpDelete("{keyword}")]
    public async Task<IActionResult> Remove(string keyword, CancellationToken ct)
    {
        try
        {
            var uid = _currentUser.GetUserId(User);
            await _service.RemoveAsync(uid, keyword, ct);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpPost("natural-language")]
    public async Task<IActionResult> FromNaturalLanguage([FromBody] NLPreferenceRequest req, CancellationToken ct)
    {
        try
        {
            var uid = _currentUser.GetUserId(User);
            var (spec, saved, total) = await _service.FromNaturalLanguageAsync(uid, req, ct);
            return Ok(new { spec, saved, total });
        }
        catch (ArgumentException aex)
        {
            return BadRequest(aex.Message);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}
