using Furina.Infrastructure.Notifications;

namespace Furina.Api.Jobs;

/// <summary>TASK-24: "job chạy 18:00 hàng ngày" — same plain-BackgroundService approach as TASK-20.</summary>
public class AppointmentReminderBackgroundService(
    AppointmentReminderJob job, ILogger<AppointmentReminderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = new DateTimeOffset(now.Year, now.Month, now.Day, 18, 0, 0, TimeSpan.Zero);
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
                logger.LogInformation("Appointment reminder job created {Count} reminder(s)", created);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Appointment reminder job failed");
            }
        }
    }
}
