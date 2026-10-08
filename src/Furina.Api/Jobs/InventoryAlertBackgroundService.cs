using Furina.Infrastructure.Notifications;

namespace Furina.Api.Jobs;

/// <summary>TASK-28: daily inventory near-expiry/expired/low-stock scan, same plain-BackgroundService pattern as TASK-20/24.</summary>
public class InventoryAlertBackgroundService(
    InventoryAlertJob job, ILogger<InventoryAlertBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = new DateTimeOffset(now.Year, now.Month, now.Day, 6, 0, 0, TimeSpan.Zero);
            if (nextRun <= now) nextRun = nextRun.AddDays(1);

            try
            {
                await Task.Delay(nextRun - now, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                var created = await job.RunAsync(stoppingToken);
                logger.LogInformation("Inventory alert job created {Count} alert(s)", created);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Inventory alert job failed");
            }
        }
    }
}
