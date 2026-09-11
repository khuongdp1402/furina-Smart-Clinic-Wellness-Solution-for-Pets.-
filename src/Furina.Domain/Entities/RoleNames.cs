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

    public static readonly string[] All = [Owner, Vet, Receptionist, SuperAdmin];
}
