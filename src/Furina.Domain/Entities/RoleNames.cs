namespace Furina.Domain.Entities;

/// <summary>
/// TASK-12 DoD: "Role tối thiểu: Owner, Vet, Receptionist, SuperAdmin".
/// Plain string constants rather than an enum so they map directly to
/// `roles.name` rows (tenant-scoped, seeded per tenant) and to the JWT
/// "role" claim without a lookup table.
/// </summary>
public static class RoleNames
{
    public const string Owner = "Owner";
    public const string Vet = "Vet";
    public const string Receptionist = "Receptionist";
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>
    /// TASK-18: a pet-owning customer, not clinic staff. Added here (not a
    /// new identity system) so AC-3's "chủ nuôi A can't see chủ nuôi B's
    /// pet" is testable against the JWT/tenant infra TASK-12 already
    /// built — logs in the same way staff do, just with this role instead.
    /// </summary>
    public const string Customer = "Customer";

    public static readonly string[] All = [Owner, Vet, Receptionist, SuperAdmin, Customer];
}
