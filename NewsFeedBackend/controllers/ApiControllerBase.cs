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
                Logger.LogWarning(ex, "{Operation} failed code={Code}", operation, ex.Code);

                var status = ex.StatusCode;
                var code   = string.IsNullOrWhiteSpace(ex.Code) ? "app/error" : ex.Code!;
                var detail = SafeDetailFor(code);

                var problem = new ProblemDetails
                {
                    Title  = code,
                    Detail = detail,
                    Status = status
                };
                problem.Extensions["operation"] = operation;

                return StatusCode(status, problem);
            }
            catch (DbUpdateException ex)
            {
                Logger.LogError(ex, "Database error in {Operation}", operation);
                return Problem(statusCode: 500, title: "Database error", detail: "Failed to persist changes.");
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError(ex, "Upstream HTTP error in {Operation}", operation);
                return Problem(statusCode: 502, title: "Upstream service error", detail: "A dependency failed.");
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogWarning(ex, "Timeout in {Operation}", operation);
                return Problem(statusCode: 504, title: "Operation timed out", detail: "Request took too long.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled error in {Operation}", operation);
                return Problem(statusCode: 500, title: "Unexpected error", detail: "Something went wrong.");
            }
            finally { sw.Stop(); }
        }
    }
    private static string SafeDetailFor(string code)
    {
        if (code.StartsWith("auth/",  StringComparison.OrdinalIgnoreCase)) return "Authentication failed.";
        if (code.StartsWith("prefs/", StringComparison.OrdinalIgnoreCase)) return "Preference operation failed.";
        if (code.StartsWith("news/",  StringComparison.OrdinalIgnoreCase)) return "Upstream news service failed.";
        if (code.StartsWith("email/", StringComparison.OrdinalIgnoreCase)) return "Email operation failed.";
        return "Request failed.";
    }
}
