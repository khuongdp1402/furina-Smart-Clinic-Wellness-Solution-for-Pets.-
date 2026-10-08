namespace Furina.Domain.Entities;

/// <summary>
/// TASK-17: a tenant-wide service definition (khám, tiêm phòng, spa...)
/// with a default price/duration every clinic sees unless it overrides
/// via <see cref="ClinicServicePrice"/>.
/// </summary>
public class ServiceCatalog : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DefaultPrice { get; set; }
    public int DefaultDurationMinutes { get; set; }
    public bool IsArchived { get; set; }

    public Tenant Tenant { get; set; } = null!;
}

/// <summary>
/// Per-clinic price/duration override for one <see cref="ServiceCatalog"/>
/// entry. A clinic with no row here for a given service uses the
/// catalog's default — this table only ever holds exceptions.
/// </summary>
public class ClinicServicePrice : ITenantScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid ServiceCatalogId { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }

    public Clinic Clinic { get; set; } = null!;
    public ServiceCatalog ServiceCatalog { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
