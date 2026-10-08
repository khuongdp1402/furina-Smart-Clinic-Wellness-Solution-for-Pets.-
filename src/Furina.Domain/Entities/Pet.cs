namespace Furina.Domain.Entities;

/// <summary>
/// TASK-18: Digital Pet ID — the record every visit, prescription, and
/// appointment in the rest of the system hangs off. Ownership (OwnerId)
/// is enforced independently of tenant membership — see TASK-18 AC-3:
/// two customers under the same tenant must not see each other's pets.
/// </summary>
public class Pet : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? MicrochipId { get; set; }

    public User Owner { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public ICollection<PetWeightLog> WeightLogs { get; set; } = [];
}

/// <summary>One weight measurement, timestamped, for a pet's growth history.</summary>
public class PetWeightLog : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PetId { get; set; }
    public decimal WeightKg { get; set; }
    public DateTimeOffset MeasuredAt { get; set; }

    public Pet Pet { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
