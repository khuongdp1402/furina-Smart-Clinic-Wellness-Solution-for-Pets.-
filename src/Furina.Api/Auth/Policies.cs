using Furina.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace Furina.Api.Auth;

/// <summary>
/// Named ASP.NET Core authorization policies, one per minimum role from
/// TASK-12. SuperAdmin is included in every policy — it's the cross-tenant
/// operator role and should never be locked out by a tenant-role check.
/// </summary>
public static class Policies
{
    public const string OwnerOnly = "OwnerOnly";
    public const string VetOnly = "VetOnly";
    public const string ReceptionistOnly = "ReceptionistOnly";

    public static void AddFurinaPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(OwnerOnly, p => p.RequireRole(RoleNames.Owner, RoleNames.SuperAdmin));
        options.AddPolicy(VetOnly, p => p.RequireRole(RoleNames.Vet, RoleNames.SuperAdmin));
        options.AddPolicy(ReceptionistOnly, p => p.RequireRole(RoleNames.Receptionist, RoleNames.SuperAdmin));
    }
}
