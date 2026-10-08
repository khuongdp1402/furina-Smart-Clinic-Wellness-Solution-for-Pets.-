namespace Furina.Domain.Entities;

/// <summary>
/// TASK-16: a user's assignment to a clinic (e.g. "Dr. X works at Clinic
/// Y"). Distinct from the RBAC <see cref="Role"/>/<see cref="UserRole"/>
/// system in TASK-12 — <see cref="JobTitle"/> here is just a display label
/// ("Vet", "Groomer"), not an authorization concept.
/// </summary>
public class Staff : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid ClinicId { get; set; }
    public string JobTitle { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    public Clinic Clinic { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Shift> Shifts { get; set; } = [];
}
