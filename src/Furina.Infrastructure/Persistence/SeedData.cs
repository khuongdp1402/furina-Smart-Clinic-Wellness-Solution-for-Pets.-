using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace Furina.Infrastructure.Persistence;

/// <summary>
/// TASK-11 DoD: "Seed script chạy lại nhiều lần không lỗi (idempotent)".
/// Creates one sample tenant, one role per TASK-12 minimum role set, and
/// one user per role (all sharing the dev-only password below) — safe to
/// run on every startup, every step checks for existence first.
/// </summary>
public static class SeedData
{
    public const string DefaultTenantSlug = "demo-clinic";
    public const string SeedPassword = "ChangeMe123!";

    public static string EmailFor(string role) => $"{role.ToLowerInvariant()}@demo-clinic.furina.local";

    public static async Task SeedAsync(FurinaDbContext db, ITenantContext tenantContext, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == DefaultTenantSlug, ct);
        if (tenant is null)
        {
            tenant = new Tenant { Name = "Demo Clinic", Slug = DefaultTenantSlug };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(ct);
        }

        // roles/users/user_roles are RLS-protected: every command needs
        // app.tenant_id on its session. EF opens/closes the underlying
        // connection between commands (pooling resets session state each
        // time), so a one-off `SET` doesn't survive — instead we set the
        // ambient tenant context and let TenantConnectionInterceptor
        // reapply `SET app.tenant_id` on every connection open, exactly
        // like it does for a real request.
        tenantContext.SetTenant(tenant.Id);

        foreach (var roleName in RoleNames.All)
        {
            await EnsureUserWithRoleAsync(db, tenant.Id, roleName, ct);
        }
    }

    private static async Task EnsureUserWithRoleAsync(FurinaDbContext db, Guid tenantId, string roleName, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == roleName, ct);
        if (role is null)
        {
            role = new Role { TenantId = tenantId, Name = roleName };
            db.Roles.Add(role);
            await db.SaveChangesAsync(ct);
        }

        var email = EmailFor(roleName);
        var user = await db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email, ct);
        if (user is null)
        {
            user = new User
            {
                TenantId = tenantId,
                Email = email,
                FullName = $"Demo {roleName}",
                // Dev-only seed password, same for every seeded user.
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(SeedPassword),
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }

        var hasRole = await db.UserRoles.AnyAsync(
            ur => ur.TenantId == tenantId && ur.UserId == user.Id && ur.RoleId == role.Id, ct);
        if (!hasRole)
        {
            db.UserRoles.Add(new UserRole { TenantId = tenantId, UserId = user.Id, RoleId = role.Id });
            await db.SaveChangesAsync(ct);
        }
    }
}
