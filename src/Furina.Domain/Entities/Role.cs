namespace Furina.Domain.Entities;

/// <summary>
/// A role within a tenant (e.g. "Admin", "Vet", "Receptionist"). Roles are
/// tenant-scoped so each clinic can define its own role set.
/// </summary>
public class Role : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Tenant Tenant { get; set; } = null!;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
