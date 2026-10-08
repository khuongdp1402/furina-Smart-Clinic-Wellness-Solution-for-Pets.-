namespace Furina.Domain.Entities;

/// <summary>
/// TASK-22: a booked appointment slot. Started as a minimal placeholder
/// in Sprint 2 (TASK-15/17 needed "does this clinic/service have a
/// future appointment" checks before Sprint 3 existed) — now fleshed out
/// with the real booking fields. StartTime/EndTime replace the old
/// single ScheduledAt; EndTime is computed from the service's duration
/// at booking time (AC-3), not recomputed later even if the catalog price
/// changes afterward.
/// </summary>
public class Appointment : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid PetId { get; set; }
    public Guid? ServiceCatalogId { get; set; }
    public Guid? VetUserId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Status { get; set; } = AppointmentStatuses.Booked;

    public Clinic Clinic { get; set; } = null!;
    public Pet Pet { get; set; } = null!;
    public ServiceCatalog? ServiceCatalog { get; set; }
    public User? VetUser { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
