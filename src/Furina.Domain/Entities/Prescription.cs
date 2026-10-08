namespace Furina.Domain.Entities;

/// <summary>
/// TASK-21: an electronic prescription for one visit — 1:1 with that
/// visit's <see cref="MedicalRecord"/> (both key off the same VisitId;
/// there is no direct FK between the two, matching the spec's "liên kết
/// 1-1 ... của cùng visit_id").
/// </summary>
public class Prescription : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid VisitId { get; set; }
    public Guid PetId { get; set; }
    public Guid VetUserId { get; set; }
    public List<PrescriptionItem> Items { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Visit Visit { get; set; } = null!;
    public Pet Pet { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}

public class PrescriptionItem
{
    public string DrugName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int DurationDays { get; set; }
}
