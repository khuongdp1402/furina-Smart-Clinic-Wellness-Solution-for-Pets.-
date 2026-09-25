using Furina.Domain.Entities;
using Furina.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Furina.Infrastructure.Inventory;

public record FefoIssueLine(Guid BatchId, string BatchNo, DateOnly ExpiryDate, int QuantityTaken);

/// <summary>Thrown when an item doesn't have enough non-expired stock to cover a requested quantity.</summary>
public class InsufficientStockException(Guid inventoryItemId, int requested, int available)
    : Exception($"Requested {requested} but only {available} available (excluding expired batches).")
{
    public Guid InventoryItemId { get; } = inventoryItemId;
    public int Requested { get; } = requested;
    public int Available { get; } = available;
}

/// <summary>
/// TASK-26's FEFO deduction, extracted so TASK-27's invoice/POS flow can
/// reuse the exact same logic inside its own transaction instead of
/// duplicating it — the caller is responsible for the transaction and the
/// advisory lock (both TASK-26's InventoryController and TASK-27's
/// InvoicesController already need their own `pg_advisory_xact_lock`
/// calls around this for their respective concurrency guarantees, so
/// locking here too would just be redundant, not wrong, but the caller's
/// lock already covers it).
/// </summary>
public static class InventoryFefoService
{
    public static async Task<List<FefoIssueLine>> IssueAsync(
        FurinaDbContext db, Guid inventoryItemId, int quantity, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var batches = await db.InventoryBatches
            .Where(b => b.InventoryItemId == inventoryItemId && b.QuantityRemaining > 0 && b.ExpiryDate >= today)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);

        var totalAvailable = batches.Sum(b => b.QuantityRemaining);
        if (totalAvailable < quantity)
        {
            throw new InsufficientStockException(inventoryItemId, quantity, totalAvailable);
        }

        var remaining = quantity;
        var result = new List<FefoIssueLine>();
        foreach (var batch in batches)
        {
            if (remaining <= 0) break;
            var take = Math.Min(remaining, batch.QuantityRemaining);
            batch.QuantityRemaining -= take;
            remaining -= take;
            result.Add(new FefoIssueLine(batch.Id, batch.BatchNo, batch.ExpiryDate, take));
        }

        return result;
    }
}
