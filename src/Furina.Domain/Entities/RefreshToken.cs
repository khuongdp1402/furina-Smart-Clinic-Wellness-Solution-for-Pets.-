namespace Furina.Domain.Entities;

/// <summary>
/// A refresh token issued to a user for JWT renewal. Tenant-scoped so RLS
/// prevents one tenant's tokens from ever being visible to another.
/// </summary>
public class RefreshToken : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
