namespace Furina.Domain.Entities;

/// <summary>TASK-26: one stock-keeping item (a drug, a supply) at a clinic.</summary>
public class InventoryItem : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    /// <summary>TASK-27: unit sell price — POS needs this to bill for physical items, not just services.</summary>
    public decimal Price { get; set; }

    /// <summary>
    /// TASK-28: total remaining stock across all batches below this triggers a
    /// low-stock alert. 0 (the default) means the check is disabled for this item.
    /// </summary>
    public int MinStockThreshold { get; set; }

    public Clinic Clinic { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public ICollection<InventoryBatch> Batches { get; set; } = [];
}

public static class InventoryAlertTypes
{
    public const string NearExpiry = "NearExpiry";
    public const string Expired = "Expired";
    public const string LowStock = "LowStock";
}

/// <summary>
/// TASK-28: a standing alert for one batch (NearExpiry/Expired) or item
/// (LowStock). The daily scan job never creates a second unresolved alert
/// for the same (Type, target) pair — that's what stops the same
/// near-expiring batch from re-alerting every single day (alert fatigue).
/// It resolves an alert itself once the underlying condition clears (stock
/// restocked above threshold, batch fully consumed), and separately closes
/// a NearExpiry alert and opens an Expired one once a batch's expiry date
/// has actually passed — expired is a distinct, higher-priority state, not
/// a continuation of "near expiry".
/// </summary>
public class InventoryAlert : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? InventoryBatchId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }

    public InventoryBatch? InventoryBatch { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public Tenant Tenant { get; set; } = null!;
}

/// <summary>
/// One received batch of an <see cref="InventoryItem"/>. Issuing stock
/// (TASK-26 FEFO — First Expired, First Out) deducts from the batch with
/// the soonest ExpiryDate first, never from an already-expired batch.
/// </summary>
public class InventoryBatch : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid InventoryItemId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public DateOnly ReceivedDate { get; set; }
    public int QuantityRemaining { get; set; }

    public InventoryItem InventoryItem { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
