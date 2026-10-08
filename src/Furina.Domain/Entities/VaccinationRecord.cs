namespace Furina.Domain.Entities;

/// <summary>TASK-20: one vaccination event, with when the next dose is due.</summary>
public class VaccinationRecord : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PetId { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateOnly DateGiven { get; set; }
    public DateOnly NextDueDate { get; set; }
    public Guid CreatedByUserId { get; set; }

    public Pet Pet { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}

/// <summary>
/// TASK-20: one row per (VaccinationRecordId, MilestoneDay) reminder ever
/// sent. Its unique index is what actually stops the daily job from
/// double-notifying if it runs twice in one day (AC-2) — keying by the
/// specific vaccination record (not just pet+vaccine name) also means a
/// re-vaccination (new record, new NextDueDate) naturally gets its own
/// fresh set of milestones instead of being blocked by the old cycle's
/// rows (test case 3).
/// </summary>
public class NotificationLog : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid VaccinationRecordId { get; set; }
    public int MilestoneDay { get; set; }
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public VaccinationRecord VaccinationRecord { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
