using Furina.Api.Auth;
using Furina.Domain.Entities;
using Furina.Infrastructure.Inventory;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Notifications;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record InventoryItemRequest(string Name, string Unit, decimal Price, int MinStockThreshold = 0);

public record InventoryItemResponse(Guid Id, Guid ClinicId, string Name, string Unit, decimal Price, int MinStockThreshold)
{
    public static InventoryItemResponse From(InventoryItem i) => new(i.Id, i.ClinicId, i.Name, i.Unit, i.Price, i.MinStockThreshold);
}

public record InventoryAlertResponse(Guid Id, string Type, Guid? InventoryBatchId, Guid? InventoryItemId, DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt)
{
    public static InventoryAlertResponse From(InventoryAlert a) => new(a.Id, a.Type, a.InventoryBatchId, a.InventoryItemId, a.CreatedAt, a.ResolvedAt);
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
public class InventoryController(FurinaDbContext db, ITenantContext tenantContext, InventoryAlertJob alertJob) : ControllerBase
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
            Price = request.Price,
            MinStockThreshold = request.MinStockThreshold,
        };
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(ct);

        return InventoryItemResponse.From(item);
    }

    /// <summary>TASK-28 ops/testing: run the near-expiry/expired/low-stock scan now instead of waiting for its daily schedule.</summary>
    [HttpPost("api/inventory-alerts/run-scan")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<ActionResult<object>> RunAlertScanNow(CancellationToken ct)
    {
        var created = await alertJob.RunAsync(ct);
        return new { created };
    }

    /// <summary>TASK-28: standing near-expiry/expired/low-stock alerts for a clinic, unresolved ones first.</summary>
    [HttpGet("api/clinics/{clinicId:guid}/inventory-alerts")]
    public async Task<ActionResult<List<InventoryAlertResponse>>> ListAlerts(Guid clinicId, CancellationToken ct)
    {
        var alerts = await db.InventoryAlerts
            .Where(a =>
                (a.InventoryBatch != null && a.InventoryBatch.InventoryItem.ClinicId == clinicId) ||
                (a.InventoryItem != null && a.InventoryItem.ClinicId == clinicId))
            .OrderBy(a => a.ResolvedAt != null)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
        return alerts.Select(InventoryAlertResponse.From).ToList();
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

        List<FefoIssueLine> issued;
        try
        {
            // AC-1 (FEFO, not FIFO) + test case 3 (never an expired
            // batch): shared with TASK-27's invoice/POS flow so both
            // deduct stock the exact same way.
            issued = await InventoryFefoService.IssueAsync(db, itemId, request.Quantity, ct);
        }
        catch (InsufficientStockException ex)
        {
            await transaction.RollbackAsync(ct);
            return BadRequest(new
            {
                error = "insufficient_stock",
                detail = $"Yêu cầu xuất {ex.Requested} nhưng chỉ còn {ex.Available} (không tính lô đã hết hạn).",
                availableQuantity = ex.Available,
            });
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var issuedFrom = issued.Select(l => new IssuedFromBatch(l.BatchId, l.BatchNo, l.ExpiryDate, l.QuantityTaken)).ToList();
        return new IssueStockResponse(request.Quantity, issuedFrom);
    }
}
