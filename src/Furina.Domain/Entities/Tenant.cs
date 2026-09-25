namespace Furina.Domain.Entities;

/// <summary>
/// A tenant (clinic organization) in the multi-tenant system. This is the root
/// of tenant isolation — every other business table carries a TenantId that
/// points back here, enforced by Postgres Row-Level Security.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>TASK-28: how many days ahead of a batch's expiry the daily scan job raises a NearExpiry alert.</summary>
    public int LowStockAlertLeadDays { get; set; } = 7;

    public ICollection<User> Users { get; set; } = new List<User>();
}
