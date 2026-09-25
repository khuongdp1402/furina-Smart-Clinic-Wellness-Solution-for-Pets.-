using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Furina.Infrastructure.Notifications;

/// <summary>
/// TASK-24: scans every tenant's Booked appointments happening "tomorrow"
/// and writes one AppointmentReminderLog per appointment newly reminded.
/// Same cross-tenant DI-scope-per-tenant shape as TASK-20's
/// VaccinationReminderJob, for the same reason — a background job has no
/// per-request ITenantContext to lean on.
/// </summary>
public class AppointmentReminderJob(IServiceScopeFactory scopeFactory)
{
    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);
        var rangeStart = new DateTimeOffset(tomorrow.Year, tomorrow.Month, tomorrow.Day, 0, 0, 0, TimeSpan.Zero);
        var rangeEnd = rangeStart.AddDays(1);

        List<Guid> tenantIds;
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FurinaDbContext>();
            tenantIds = await db.Tenants.Select(t => t.Id).ToListAsync(ct);
        }

        var totalCreated = 0;
        foreach (var tenantId in tenantIds)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FurinaDbContext>();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.SetTenant(tenantId);

            // AC-2: only Booked appointments qualify — a Cancelled one
            // (even if it was Booked when the appointment was made) is
            // filtered out by this WHERE clause every single run, so it
            // never gets reminded no matter how many times the job fires.
            var dueAppointments = await db.Appointments
                .Where(a => a.Status == AppointmentStatuses.Booked
                    && a.StartTime >= rangeStart && a.StartTime < rangeEnd)
                .ToListAsync(ct);

            foreach (var appointment in dueAppointments)
            {
                var alreadyReminded = await db.AppointmentReminderLogs
                    .AnyAsync(l => l.AppointmentId == appointment.Id, ct);
                if (alreadyReminded) continue; // test case 3: re-run must not duplicate.

                db.AppointmentReminderLogs.Add(new AppointmentReminderLog
                {
                    TenantId = tenantId,
                    AppointmentId = appointment.Id,
                });
                totalCreated++;
            }

            await db.SaveChangesAsync(ct);
        }

        return totalCreated;
    }
}
