using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NewsFeedBackend.Http
{
    public class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHandler> _logger;
        public LoggingHandler(ILogger<LoggingHandler> logger) => _logger = logger;

        // redact common secret-y query params
        private static readonly Regex SecretQuery =
            new("(apikey|api_key|token|key|password)=([^&]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            HttpResponseMessage res;
            try
            {
                res = await base.SendAsync(req, ct);
            }
            finally
            {
                sw.Stop();
            }

            var url = req.RequestUri?.ToString() ?? "";
            url = SecretQuery.Replace(url, m => $"{m.Groups[1].Value}=***");

            var status = (int)res.StatusCode;
            var level = status >= 500 ? LogLevel.Error
                      : status >= 400 ? LogLevel.Warning
                      : LogLevel.Information; // keep 2xx/3xx but short

            _logger.Log(level, "HTTP OUT {Status} {Method} {Url} in {ElapsedMs}ms",
                status, req.Method.Method, url, sw.ElapsedMilliseconds);

            return res;
        }
    }
}
