using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Furina.Infrastructure.Notifications;

/// <summary>
/// TASK-28: daily scan for near-expiry / expired batches and low-stock
/// items. Dedupe is by an unresolved <see cref="InventoryAlert"/> row for
/// the same (Type, target) pair, not by "did we run today" — that's what
/// keeps a batch that's been near-expiry for a week from generating a
/// fresh alert every single run (alert fatigue), while still re-alerting
/// once the underlying condition changes (e.g. stock restocked, then runs
/// low again). Runs across ALL tenants like the other reminder jobs, so it
/// opens its own DI scope (and ITenantContext) per tenant.
/// </summary>
public class InventoryAlertJob(IServiceScopeFactory scopeFactory)
{
    public async Task<int> RunAsync(CancellationToken ct = default)
    {
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

            totalCreated += await RunForCurrentTenantAsync(db, ct);
        }

        return totalCreated;
    }

    private static async Task<int> RunForCurrentTenantAsync(FurinaDbContext db, CancellationToken ct)
    {
        var created = 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var tenant = await db.Tenants.FirstAsync(ct);
        var leadDays = tenant.LowStockAlertLeadDays;

        var openAlerts = await db.InventoryAlerts.Where(a => a.ResolvedAt == null).ToListAsync(ct);

        // Test case 3: a batch whose expiry date has actually passed moves
        // to Expired and stops being reported as NearExpiry — close any
        // open NearExpiry alert for it here, rather than letting both
        // exist side by side.
        var batches = await db.InventoryBatches.Where(b => b.QuantityRemaining > 0).ToListAsync(ct);
        foreach (var batch in batches)
        {
            var openNearExpiry = openAlerts.FirstOrDefault(a =>
                a.Type == InventoryAlertTypes.NearExpiry && a.InventoryBatchId == batch.Id);
            var openExpired = openAlerts.FirstOrDefault(a =>
                a.Type == InventoryAlertTypes.Expired && a.InventoryBatchId == batch.Id);

            if (batch.ExpiryDate < today)
            {
                if (openNearExpiry is not null) openNearExpiry.ResolvedAt = DateTimeOffset.UtcNow;
                if (openExpired is null)
                {
                    db.InventoryAlerts.Add(new InventoryAlert
                    {
                        TenantId = batch.TenantId,
                        Type = InventoryAlertTypes.Expired,
                        InventoryBatchId = batch.Id,
                    });
                    created++;
                }
            }
            else if (batch.ExpiryDate <= today.AddDays(leadDays))
            {
                // AC-1: don't duplicate — an alert already open for this
                // exact batch means nothing new to report today.
                if (openNearExpiry is null)
                {
                    db.InventoryAlerts.Add(new InventoryAlert
                    {
                        TenantId = batch.TenantId,
                        Type = InventoryAlertTypes.NearExpiry,
                        InventoryBatchId = batch.Id,
                    });
                    created++;
                }
                if (openExpired is not null) openExpired.ResolvedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                // No longer within the lead window (e.g. a fresh batch
                // replaced the near-expiry one) — clear any stale alert.
                if (openNearExpiry is not null) openNearExpiry.ResolvedAt = DateTimeOffset.UtcNow;
            }
        }

        // AC-2: low stock is its own alert type, keyed by item, separate
        // from the batch-level expiry alerts above.
        var items = await db.InventoryItems.Where(i => i.MinStockThreshold > 0).ToListAsync(ct);
        foreach (var item in items)
        {
            var totalRemaining = await db.InventoryBatches
                .Where(b => b.InventoryItemId == item.Id)
                .SumAsync(b => b.QuantityRemaining, ct);

            var openLowStock = openAlerts.FirstOrDefault(a =>
                a.Type == InventoryAlertTypes.LowStock && a.InventoryItemId == item.Id);

            if (totalRemaining < item.MinStockThreshold)
            {
                if (openLowStock is null)
                {
                    db.InventoryAlerts.Add(new InventoryAlert
                    {
                        TenantId = item.TenantId,
                        Type = InventoryAlertTypes.LowStock,
                        InventoryItemId = item.Id,
                    });
                    created++;
                }
            }
            else if (openLowStock is not null)
            {
                openLowStock.ResolvedAt = DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);
        return created;
    }
}
