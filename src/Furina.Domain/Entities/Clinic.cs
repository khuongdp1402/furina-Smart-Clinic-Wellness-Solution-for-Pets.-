namespace Furina.Domain.Entities;

/// <summary>
/// A physical clinic location belonging to a tenant. TASK-15: the base
/// every other operational module (appointments, staff, inventory) hangs
/// off via ClinicId.
/// </summary>
public class Clinic : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool Is24hEmergency { get; set; }

    /// <summary>
    /// Serialized as JSON in the DB (see FurinaDbContext's OwnsMany/JSON
    /// column mapping) — one entry per day of week, 0=Sunday..6=Saturday.
    /// </summary>
    public List<OpeningHour> OpeningHours { get; set; } = [];

    /// <summary>
    /// Soft-delete flag (TASK-15 AC-3): an archived clinic is hidden from
    /// normal listings but its historical appointments/records stay
    /// intact — DELETE never hard-deletes a clinic with future
    /// appointments.
    /// </summary>
    public bool IsArchived { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

public class OpeningHour
{
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
}
