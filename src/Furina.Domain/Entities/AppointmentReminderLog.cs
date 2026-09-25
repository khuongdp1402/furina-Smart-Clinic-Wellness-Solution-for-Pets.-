namespace Furina.Domain.Entities;

/// <summary>
/// TASK-24: one row per appointment ever reminded — unique on
/// AppointmentId (unlike TASK-20's vaccination reminders, this is a
/// single "day before" nudge, not several milestones, so there's no
/// extra milestone key needed). Same role as TASK-20's NotificationLog:
/// the unique index, not any in-memory state, is what makes a job re-run
/// safe.
/// </summary>
public class AppointmentReminderLog : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid AppointmentId { get; set; }
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public Appointment Appointment { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
