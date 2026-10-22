using System.Diagnostics;
using Microsoft.Extensions.Options;
using WMS.Infrastructure.Demo;

namespace WMS.API.Services;

public sealed class DemoCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<DemoOptions> options,
    ILogger<DemoCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield immediately so the first cleanup does not delay API startup.
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            if (options.Value.CleanupEnabled)
            {
                var timer = Stopwatch.StartNew();
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var cleanup = scope.ServiceProvider.GetRequiredService<DemoCleanupRunner>();
                    var result = await cleanup.RunAsync(options.Value.CleanupBatchSize, stoppingToken);
                    logger.LogInformation(
                        "Demo cleanup completed: {Attempted} candidates, {Deleted} deleted, {Skipped} skipped, {Failed} failed in {ElapsedMilliseconds:F2} ms.",
                        result.Deleted + result.Skipped + result.Failed,
                        result.Deleted, result.Skipped, result.Failed, timer.Elapsed.TotalMilliseconds);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception,
                        "Demo cleanup failed after {ElapsedMilliseconds:F2} ms; it will retry at the next UTC hour.",
                        timer.Elapsed.TotalMilliseconds);
                }
            }

            var now = DateTimeOffset.UtcNow;
            var nextHour = new DateTimeOffset(now.UtcDateTime.Date.AddHours(now.Hour + 1), TimeSpan.Zero);
            try { await Task.Delay(nextHour - now, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
