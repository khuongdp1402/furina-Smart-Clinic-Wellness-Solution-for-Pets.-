namespace Furina.Domain.Entities;

/// <summary>
/// Join table between User and Role, tenant-scoped like everything else.
/// </summary>
public class UserRole : ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
