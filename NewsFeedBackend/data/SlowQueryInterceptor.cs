using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NewsFeedBackend.Data;

public sealed class SlowQueryInterceptor : DbCommandInterceptor
{
    private readonly ILogger<SlowQueryInterceptor> _logger;
    private readonly int _thresholdMs;
    private readonly ConcurrentDictionary<Guid, Stopwatch> _timers = new();

    public SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger, int thresholdMs = 500)
    {
        _logger = logger;
        _thresholdMs = thresholdMs;
    }

    private void Start(Guid id) => _timers.TryAdd(id, Stopwatch.StartNew());

    private void StopAndMaybeLog(Guid id, string sql)
    {
        if (_timers.TryRemove(id, out var sw))
        {
            sw.Stop();
            var ms = sw.ElapsedMilliseconds;
            if (ms >= _thresholdMs)
                _logger.LogWarning("SLOW SQL ({ElapsedMs} ms): {Sql}", ms, sql);
            else
                _logger.LogDebug("SQL ({ElapsedMs} ms)", ms);
        }
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    { Start(eventData.CommandId); return base.NonQueryExecuting(command, eventData, result); }

    public override int NonQueryExecuted(
        DbCommand command, CommandExecutedEventData eventData, int result)
    { StopAndMaybeLog(eventData.CommandId, command.CommandText); return base.NonQueryExecuted(command, eventData, result); }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    { Start(eventData.CommandId); return base.ReaderExecuting(command, eventData, result); }

    public override DbDataReader ReaderExecuted(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    { StopAndMaybeLog(eventData.CommandId, command.CommandText); return base.ReaderExecuted(command, eventData, result); }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    { Start(eventData.CommandId); return base.ScalarExecuting(command, eventData, result); }

    public override object ScalarExecuted(
        DbCommand command, CommandExecutedEventData eventData, object result)
    { StopAndMaybeLog(eventData.CommandId, command.CommandText); return base.ScalarExecuted(command, eventData, result); }
}
