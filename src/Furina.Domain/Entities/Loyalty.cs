namespace Furina.Domain.Entities;

public static class LoyaltyTransactionTypes
{
    public const string Earn = "Earn";
    public const string Redeem = "Redeem";
    public const string Refund = "Refund";
}

/// <summary>TASK-29: one customer's running points balance. OwnerId is a User (the Customer role from TASK-18).</summary>
public class LoyaltyAccount : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OwnerId { get; set; }
    public int PointsBalance { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<LoyaltyTransaction> Transactions { get; set; } = [];
}

/// <summary>
/// TASK-29: one ledger entry against a LoyaltyAccount. Points is the signed
/// delta actually applied to the balance — positive for Earn/Refund,
/// negative for Redeem — so the balance is always re-derivable as the sum
/// of this table, never a value trusted independently of the ledger.
/// </summary>
public class LoyaltyTransaction : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid LoyaltyAccountId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Points { get; set; }
    public Guid? InvoiceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public LoyaltyAccount LoyaltyAccount { get; set; } = null!;
    public Invoice? Invoice { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
