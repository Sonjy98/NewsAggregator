using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NewsFeedBackend.Http;

public class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;
    public LoggingHandler(ILogger<LoggingHandler> logger) => _logger = logger;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("HTTP OUT → {Method} {Url}", req.Method, req.RequestUri);

        try
        {
            var res = await base.SendAsync(req, ct);
            sw.Stop();
            _logger.LogInformation("HTTP OUT ← {Status} in {ElapsedMs} ms ({Method} {Url})",
                (int)res.StatusCode, sw.ElapsedMilliseconds, req.Method, req.RequestUri);
            return res;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "HTTP OUT ✖ {Method} {Url} after {ElapsedMs} ms",
                req.Method, req.RequestUri, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
