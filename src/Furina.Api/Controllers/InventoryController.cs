using Furina.Api.Auth;
using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record InventoryItemRequest(string Name, string Unit);

public record InventoryItemResponse(Guid Id, Guid ClinicId, string Name, string Unit)
{
    public static InventoryItemResponse From(InventoryItem i) => new(i.Id, i.ClinicId, i.Name, i.Unit);
}

public record ReceiveBatchRequest(string BatchNo, DateOnly ExpiryDate, DateOnly ReceivedDate, int Quantity);

public record InventoryBatchResponse(Guid Id, string BatchNo, DateOnly ExpiryDate, DateOnly ReceivedDate, int QuantityRemaining)
{
    public static InventoryBatchResponse From(InventoryBatch b) => new(b.Id, b.BatchNo, b.ExpiryDate, b.ReceivedDate, b.QuantityRemaining);
}

public record IssueStockRequest(int Quantity);

public record IssuedFromBatch(Guid BatchId, string BatchNo, DateOnly ExpiryDate, int QuantityTaken);

public record IssueStockResponse(int QuantityIssued, List<IssuedFromBatch> FromBatches);

/// <summary>
/// TASK-26: batch-tracked inventory, issued FEFO (First Expired, First
/// Out) — never FIFO, and never from an already-expired batch.
/// </summary>
[ApiController]
[Authorize]
public class InventoryController(FurinaDbContext db, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("api/clinics/{clinicId:guid}/inventory-items")]
    public async Task<ActionResult<List<InventoryItemResponse>>> ListForClinic(Guid clinicId, CancellationToken ct)
    {
        var items = await db.InventoryItems.Where(i => i.ClinicId == clinicId).ToListAsync(ct);
        return items.Select(InventoryItemResponse.From).ToList();
    }

    [HttpPost("api/clinics/{clinicId:guid}/inventory-items")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<InventoryItemResponse>> CreateItem(Guid clinicId, InventoryItemRequest request, CancellationToken ct)
    {
        var clinicExists = await db.Clinics.AnyAsync(c => c.Id == clinicId, ct);
        if (!clinicExists) return NotFound(new { error = "clinic_not_found" });

        var item = new InventoryItem
        {
            TenantId = tenantContext.TenantId!.Value,
            ClinicId = clinicId,
            Name = request.Name,
            Unit = request.Unit,
        };
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(ct);

        return InventoryItemResponse.From(item);
    }

    [HttpGet("api/inventory-items/{itemId:guid}/batches")]
    public async Task<ActionResult<List<InventoryBatchResponse>>> ListBatches(Guid itemId, CancellationToken ct)
    {
        var batches = await db.InventoryBatches
            .Where(b => b.InventoryItemId == itemId && b.QuantityRemaining > 0)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);
        return batches.Select(InventoryBatchResponse.From).ToList();
    }

    [HttpPost("api/inventory-items/{itemId:guid}/batches")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<InventoryBatchResponse>> ReceiveBatch(Guid itemId, ReceiveBatchRequest request, CancellationToken ct)
    {
        var itemExists = await db.InventoryItems.AnyAsync(i => i.Id == itemId, ct);
        if (!itemExists) return NotFound(new { error = "inventory_item_not_found" });

        var batch = new InventoryBatch
        {
            TenantId = tenantContext.TenantId!.Value,
            InventoryItemId = itemId,
            BatchNo = request.BatchNo,
            ExpiryDate = request.ExpiryDate,
            ReceivedDate = request.ReceivedDate,
            QuantityRemaining = request.Quantity,
        };
        db.InventoryBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        return InventoryBatchResponse.From(batch);
    }

    /// <summary>
    /// TASK-26 AC-1/AC-2: deducts FEFO across batches, serialized per item
    /// by an advisory lock so two concurrent issues can't both read the
    /// same "available" total and together drive stock negative — the
    /// same class of race TASK-22 solved for appointment slots.
    /// </summary>
    [HttpPost("api/inventory-items/{itemId:guid}/issue")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<IssueStockResponse>> IssueStock(Guid itemId, IssueStockRequest request, CancellationToken ct)
    {
        if (request.Quantity <= 0) return BadRequest(new { error = "quantity_must_be_positive" });

        var itemExists = await db.InventoryItems.AnyAsync(i => i.Id == itemId, ct);
        if (!itemExists) return NotFound(new { error = "inventory_item_not_found" });

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({itemId.ToString()})::bigint)", ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // AC-1 (FEFO, not FIFO) + test case 3 (never an expired batch):
        // ordered soonest-expiry-first, expired batches excluded entirely.
        var batches = await db.InventoryBatches
            .Where(b => b.InventoryItemId == itemId && b.QuantityRemaining > 0 && b.ExpiryDate >= today)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);

        var totalAvailable = batches.Sum(b => b.QuantityRemaining);
        if (totalAvailable < request.Quantity)
        {
            await transaction.RollbackAsync(ct);
            return BadRequest(new
            {
                error = "insufficient_stock",
                detail = $"Yêu cầu xuất {request.Quantity} nhưng chỉ còn {totalAvailable} (không tính lô đã hết hạn).",
                availableQuantity = totalAvailable,
            });
        }

        var remaining = request.Quantity;
        var issuedFrom = new List<IssuedFromBatch>();
        foreach (var batch in batches)
        {
            if (remaining <= 0) break;
            var take = Math.Min(remaining, batch.QuantityRemaining);
            batch.QuantityRemaining -= take;
            remaining -= take;
            issuedFrom.Add(new IssuedFromBatch(batch.Id, batch.BatchNo, batch.ExpiryDate, take));
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new IssueStockResponse(request.Quantity, issuedFrom);
    }
}
