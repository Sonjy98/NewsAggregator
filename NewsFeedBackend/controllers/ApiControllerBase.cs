using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsFeedBackend.Errors;

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
            ["Path"]      = HttpContext?.Request?.Path.Value,
            ["TraceId"]   = HttpContext?.TraceIdentifier
        }))
        {
            Logger.LogInformation("Start {Operation}", operation);
            try
            {
                var result = await action();
                Logger.LogInformation("End {Operation} in {ElapsedMs} ms", operation, sw.ElapsedMilliseconds);
                return result;
            }
            catch (AppException ex)
            {
                Logger.LogWarning(ex, "Handled {ErrorType} in {Operation}", ex.GetType().Name, operation);
                var problem = new ProblemDetails
                {
                    Title  = ex.GetType().Name,
                    Detail = ex.Message,
                    Status = ex.StatusCode
                };
                if (!string.IsNullOrWhiteSpace(ex.Code))
                    problem.Extensions["code"] = ex.Code;

                return StatusCode(ex.StatusCode, problem);
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
            finally { sw.Stop(); }
        }
    }
}
