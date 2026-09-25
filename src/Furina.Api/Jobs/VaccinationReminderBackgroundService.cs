using Furina.Infrastructure.Notifications;

namespace Furina.Api.Jobs;

/// <summary>
/// TASK-20: "job 08:00 hàng ngày". A plain BackgroundService rather than
/// pulling in Quartz.NET/Hangfire as the spec suggested — this task's ACs
/// only require correct daily-once, no-duplicate behavior, which a timer
/// loop provides without the extra dependency; swap this for a real
/// scheduler if cron-like features (retries, dashboards, misfire
/// handling) become necessary later.
/// </summary>
public class VaccinationReminderBackgroundService(
    VaccinationReminderJob job, ILogger<VaccinationReminderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = new DateTimeOffset(now.Year, now.Month, now.Day, 8, 0, 0, TimeSpan.Zero);
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
                logger.LogInformation("Vaccination reminder job created {Count} notification(s)", created);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // A failed run must not crash the host — tomorrow's run
                // should still happen.
                logger.LogError(ex, "Vaccination reminder job failed");
            }
        }
    }
}
