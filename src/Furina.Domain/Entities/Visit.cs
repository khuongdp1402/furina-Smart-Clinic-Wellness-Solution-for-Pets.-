namespace Furina.Domain.Entities;

/// <summary>
/// A single clinic visit for one pet — the anchor TASK-19's SOAP note
/// (and later TASK-20/21) attach to. Deliberately minimal: no link to
/// Appointment (walk-ins exist too, and no task in this sprint owns that
/// relationship), just enough to say "this pet was seen, here, by this
/// vet, on this date".
/// </summary>
public class Visit : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PetId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid VetUserId { get; set; }
    public DateTimeOffset VisitDate { get; set; } = DateTimeOffset.UtcNow;

    public Pet Pet { get; set; } = null!;
    public Clinic Clinic { get; set; } = null!;
    public User VetUser { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
