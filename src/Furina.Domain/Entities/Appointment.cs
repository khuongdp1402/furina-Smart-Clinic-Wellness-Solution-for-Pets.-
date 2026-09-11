namespace Furina.Domain.Entities;

/// <summary>
/// Deliberately minimal placeholder pulled forward from Sprint 3
/// (TASK-22/23) — exists in Sprint 2 only so TASK-15's AC-3 ("xoá cơ sở
/// đang có lịch hẹn tương lai không được xoá cứng") and TASK-17's test
/// case 3 (same, for a service) have something real to check against. No
/// booking logic, no state machine, no endpoints here — those are
/// TASK-22/23's job when Sprint 3 actually builds this out.
/// </summary>
public class Appointment : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid? ServiceCatalogId { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public string Status { get; set; } = "Scheduled";

    public Clinic Clinic { get; set; } = null!;
    public ServiceCatalog? ServiceCatalog { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
