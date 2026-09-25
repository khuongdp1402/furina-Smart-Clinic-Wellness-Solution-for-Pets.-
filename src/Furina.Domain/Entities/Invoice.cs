namespace Furina.Domain.Entities;

public static class InvoiceLineType
{
    public const string Service = "Service";
    public const string InventoryItem = "InventoryItem";
}

/// <summary>
/// TASK-27: a POS invoice. TotalAmount is always computed server-side
/// from each line's real unit price at billing time — never trusted from
/// the client (AC-3).
/// </summary>
public static class InvoiceStatuses
{
    public const string Active = "Active";
    public const string Cancelled = "Cancelled";
}

public class Invoice : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid? VisitId { get; set; }

    /// <summary>TASK-29: the paying customer (a Customer-role User), if any — loyalty points are only earned when this is set.</summary>
    public Guid? OwnerId { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Status { get; set; } = InvoiceStatuses.Active;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Clinic Clinic { get; set; } = null!;
    public Visit? Visit { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public ICollection<InvoiceLineItem> Lines { get; set; } = [];
}

/// <summary>
/// One line of an invoice — either a service (TASK-17's ServiceCatalog,
/// clinic-override-aware) or a physical inventory item (TASK-26, which
/// also deducts stock atomically alongside creating this line).
/// </summary>
public class InvoiceLineItem : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? ServiceCatalogId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public ServiceCatalog? ServiceCatalog { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
