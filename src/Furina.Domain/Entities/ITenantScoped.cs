namespace Furina.Domain.Entities;

/// <summary>
/// Marks an entity as tenant-scoped: it carries a TenantId column and must
/// have a Postgres RLS policy applied on its table.
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}
