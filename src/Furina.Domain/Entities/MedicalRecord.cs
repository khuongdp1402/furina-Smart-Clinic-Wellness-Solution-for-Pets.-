namespace Furina.Domain.Entities;

/// <summary>
/// TASK-19: a SOAP note (Subjective/Objective/Assessment/Plan — the
/// veterinary-industry-standard structure, per VETport) for one visit.
/// Legally-significant record: edits are locked after
/// <see cref="MedicalRecordOptions.EditLockHours"/> hours from
/// <see cref="CreatedAt"/> (see <see cref="MedicalRecordAuditLog"/> for
/// what happens to edits made while still unlocked).
/// </summary>
public class MedicalRecord : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid VisitId { get; set; }
    public Guid PetId { get; set; }
    public Guid VetUserId { get; set; }
    public string Subjective { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Visit Visit { get; set; } = null!;
    public Pet Pet { get; set; } = null!;
    public User VetUser { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}

/// <summary>
/// One row per edit made to a <see cref="MedicalRecord"/> while still
/// inside the edit window — captures the values BEFORE the edit, so the
/// record's history can be reconstructed (TASK-19 AC-3: "có audit log ghi
/// lại thay đổi").
/// </summary>
public class MedicalRecordAuditLog : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid MedicalRecordId { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string PreviousSubjective { get; set; } = string.Empty;
    public string PreviousObjective { get; set; } = string.Empty;
    public string PreviousAssessment { get; set; } = string.Empty;
    public string PreviousPlan { get; set; } = string.Empty;

    public MedicalRecord MedicalRecord { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
