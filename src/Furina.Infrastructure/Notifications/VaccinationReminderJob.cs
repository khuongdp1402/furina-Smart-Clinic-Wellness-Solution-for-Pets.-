using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Furina.Infrastructure.Notifications;

/// <summary>
/// TASK-20: scans every tenant's due vaccinations and writes one
/// <see cref="NotificationLog"/> row per (vaccination record, milestone)
/// that's newly due — the unique index on that pair is what actually
/// makes re-running this safe, not any in-memory state, so it survives a
/// worker restart mid-run. Runs across ALL tenants, so it can't rely on
/// the usual per-request <see cref="ITenantContext"/> — it opens its own
/// DI scope (and therefore its own ITenantContext) per tenant.
/// </summary>
public class VaccinationReminderJob(IServiceScopeFactory scopeFactory)
{
    private static readonly int[] MilestoneDays = [7, 3, 1];

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

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

            totalCreated += await RunForCurrentTenantAsync(db, today, ct);
        }

        return totalCreated;
    }

    private static async Task<int> RunForCurrentTenantAsync(FurinaDbContext db, DateOnly today, CancellationToken ct)
    {
        var created = 0;
        foreach (var milestone in MilestoneDays)
        {
            var targetDate = today.AddDays(milestone);

            // Test case 3: keying by VaccinationRecordId (not vaccine
            // name) means a re-vaccination — a brand new record with a
            // new NextDueDate — automatically gets its own fresh set of
            // milestones; there's no old row for the new record's id to
            // collide with.
            var dueRecords = await db.VaccinationRecords
                .Where(v => v.NextDueDate == targetDate)
                .ToListAsync(ct);

            foreach (var record in dueRecords)
            {
                var alreadySent = await db.NotificationLogs
                    .AnyAsync(n => n.VaccinationRecordId == record.Id && n.MilestoneDay == milestone, ct);
                if (alreadySent) continue; // AC-2: running twice in one day must not duplicate.

                db.NotificationLogs.Add(new NotificationLog
                {
                    TenantId = record.TenantId,
                    VaccinationRecordId = record.Id,
                    MilestoneDay = milestone,
                });
                created++;
            }
        }

        await db.SaveChangesAsync(ct);
        return created;
    }
}
