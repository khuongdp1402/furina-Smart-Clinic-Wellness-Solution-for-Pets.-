namespace Furina.Domain.Entities;

/// <summary>TASK-23 AC-3: who changed an appointment's status, when, and between which two states.</summary>
public class AppointmentStatusAuditLog : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid AppointmentId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }

    public Appointment Appointment { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
