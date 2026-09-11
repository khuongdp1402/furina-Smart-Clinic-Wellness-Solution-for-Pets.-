namespace Furina.Infrastructure.MultiTenancy;

/// <summary>
/// Holds the tenant id for the current request/scope. Populated by
/// authentication middleware (Sprint 1 task: JWT + tenant resolution
/// middleware) before any DbContext query runs.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    void SetTenant(Guid tenantId);
}

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public void SetTenant(Guid tenantId) => TenantId = tenantId;
}
