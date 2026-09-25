namespace Furina.Domain.Entities;

/// <summary>TASK-26: one stock-keeping item (a drug, a supply) at a clinic.</summary>
public class InventoryItem : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public Clinic Clinic { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public ICollection<InventoryBatch> Batches { get; set; } = [];
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
