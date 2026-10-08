using Furina.Api.Auth;
using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record ServiceRequest(string Name, decimal DefaultPrice, int DefaultDurationMinutes);

public record ServiceResponse(Guid Id, string Name, decimal DefaultPrice, int DefaultDurationMinutes, bool IsArchived)
{
    public static ServiceResponse From(ServiceCatalog s) => new(s.Id, s.Name, s.DefaultPrice, s.DefaultDurationMinutes, s.IsArchived);
}

public record ClinicServiceRequest(decimal Price, int DurationMinutes);

/// <summary>
/// The price a clinic actually charges for a service — its own override
/// if one exists, otherwise the catalog default (TASK-17 AC-1/AC-2).
/// </summary>
public record EffectiveServicePrice(Guid ServiceId, string Name, decimal Price, int DurationMinutes, bool IsOverridden);

/// <summary>
/// TASK-17: tenant-wide service catalog with per-clinic price overrides.
/// Read is open to any authenticated role; writes need
/// <see cref="Policies.ClinicManage"/>.
/// </summary>
[ApiController]
[Authorize]
public class ServicesController(FurinaDbContext db, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("api/services")]
    public async Task<ActionResult<List<ServiceResponse>>> List(CancellationToken ct)
    {
        var services = await db.ServiceCatalog.Where(s => !s.IsArchived).ToListAsync(ct);
        return services.Select(ServiceResponse.From).ToList();
    }

    [HttpPost("api/services")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<ServiceResponse>> Create(ServiceRequest request, CancellationToken ct)
    {
        var service = new ServiceCatalog
        {
            TenantId = tenantContext.TenantId!.Value,
            Name = request.Name,
            DefaultPrice = request.DefaultPrice,
            DefaultDurationMinutes = request.DefaultDurationMinutes,
        };
        db.ServiceCatalog.Add(service);
        await db.SaveChangesAsync(ct);

        // TASK-17 AC-1: "mọi cơ sở thấy dịch vụ đó ngay với giá mặc định"
        // is true automatically — no ClinicServicePrice row is created
        // here, and GetForClinic below falls back to DefaultPrice for any
        // clinic that never overrides.
        return ServiceResponse.From(service);
    }

    [HttpPut("api/services/{id:guid}")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<ServiceResponse>> Update(Guid id, ServiceRequest request, CancellationToken ct)
    {
        var service = await db.ServiceCatalog.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null) return NotFound();

        service.Name = request.Name;
        service.DefaultPrice = request.DefaultPrice;
        service.DefaultDurationMinutes = request.DefaultDurationMinutes;
        await db.SaveChangesAsync(ct);
        return ServiceResponse.From(service);
    }

    /// <summary>
    /// TASK-17 test case 3: a service still referenced by a future
    /// appointment is archived, never hard-deleted (same shape as
    /// TASK-15 AC-3 for clinics).
    /// </summary>
    [HttpDelete("api/services/{id:guid}")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var service = await db.ServiceCatalog.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null) return NotFound();

        var hasFutureAppointments = await db.Appointments
            .AnyAsync(a => a.ServiceCatalogId == id && a.StartTime > DateTimeOffset.UtcNow, ct);

        if (hasFutureAppointments)
        {
            service.IsArchived = true;
            await db.SaveChangesAsync(ct);
            return Conflict(new
            {
                error = "service_has_future_appointments",
                detail = "Service has future appointments; archived instead of deleted.",
            });
        }

        db.ServiceCatalog.Remove(service);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("api/clinics/{clinicId:guid}/services")]
    public async Task<ActionResult<List<EffectiveServicePrice>>> GetForClinic(Guid clinicId, CancellationToken ct)
    {
        var overrides = await db.ClinicServicePrices
            .Where(p => p.ClinicId == clinicId)
            .ToDictionaryAsync(p => p.ServiceCatalogId, ct);

        var services = await db.ServiceCatalog.Where(s => !s.IsArchived).ToListAsync(ct);

        return services.Select(s => overrides.TryGetValue(s.Id, out var over)
            ? new EffectiveServicePrice(s.Id, s.Name, over.Price, over.DurationMinutes, IsOverridden: true)
            : new EffectiveServicePrice(s.Id, s.Name, s.DefaultPrice, s.DefaultDurationMinutes, IsOverridden: false)
        ).ToList();
    }

    [HttpPut("api/clinics/{clinicId:guid}/services/{serviceId:guid}/price")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<EffectiveServicePrice>> SetOverride(
        Guid clinicId, Guid serviceId, ClinicServiceRequest request, CancellationToken ct)
    {
        var service = await db.ServiceCatalog.FirstOrDefaultAsync(s => s.Id == serviceId, ct);
        if (service is null) return NotFound(new { error = "service_not_found" });

        var clinicExists = await db.Clinics.AnyAsync(c => c.Id == clinicId, ct);
        if (!clinicExists) return NotFound(new { error = "clinic_not_found" });

        var over = await db.ClinicServicePrices
            .FirstOrDefaultAsync(p => p.ClinicId == clinicId && p.ServiceCatalogId == serviceId, ct);

        if (over is null)
        {
            over = new ClinicServicePrice
            {
                TenantId = tenantContext.TenantId!.Value,
                ClinicId = clinicId,
                ServiceCatalogId = serviceId,
            };
            db.ClinicServicePrices.Add(over);
        }

        over.Price = request.Price;
        over.DurationMinutes = request.DurationMinutes;
        await db.SaveChangesAsync(ct);

        return new EffectiveServicePrice(service.Id, service.Name, over.Price, over.DurationMinutes, IsOverridden: true);
    }
}
