using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace NewsFeedBackend.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly ILogger Logger;
    protected ApiControllerBase(ILogger logger) => Logger = logger;

    protected string? UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? User?.FindFirstValue("sub");

    protected async Task<IActionResult> Safe(string operation, Func<Task<IActionResult>> action)
    {
        var sw = Stopwatch.StartNew();

        using (Logger.BeginScope(new Dictionary<string, object?>
        {
            ["Operation"] = operation,
            ["Path"] = HttpContext?.Request?.Path.Value,
            ["TraceId"] = HttpContext?.TraceIdentifier
        }))
        {
            Logger.LogInformation("Start {Operation}", operation);
            try
            {
                var result = await action();
                Logger.LogInformation("End {Operation} in {ElapsedMs} ms", operation, sw.ElapsedMilliseconds);
                return result;
            }
            catch (ArgumentException ex)
            {
                Logger.LogWarning(ex, "Bad request in {Operation}", operation);
                return Problem(statusCode: 400, title: "Bad request", detail: ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                Logger.LogWarning(ex, "Not found in {Operation}", operation);
                return Problem(statusCode: 404, title: "Not found", detail: ex.Message);
            }
            catch (DbUpdateException ex)
            {
                Logger.LogError(ex, "Database error in {Operation}", operation);
                return Problem(statusCode: 500, title: "Database error", detail: "Failed to persist changes.");
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError(ex, "Upstream HTTP error in {Operation}", operation);
                return Problem(statusCode: 502, title: "Upstream service error", detail: ex.Message);
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogWarning(ex, "Timeout in {Operation}", operation);
                return Problem(statusCode: 504, title: "Operation timed out", detail: ex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled error in {Operation}", operation);
                return Problem(statusCode: 500, title: "Unexpected error", detail: "Something went wrong.");
            }
            finally
            {
                sw.Stop();
            }
        }
    }
}
