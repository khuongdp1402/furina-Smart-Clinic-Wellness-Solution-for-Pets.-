using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record CreateAppointmentRequest(Guid PetId, Guid ClinicId, Guid ServiceCatalogId, Guid? VetUserId, DateTimeOffset StartTime);

public record AppointmentResponse(
    Guid Id, Guid ClinicId, Guid PetId, Guid? ServiceCatalogId, Guid? VetUserId,
    DateTimeOffset StartTime, DateTimeOffset EndTime, string Status)
{
    public static AppointmentResponse From(Appointment a) => new(
        a.Id, a.ClinicId, a.PetId, a.ServiceCatalogId, a.VetUserId, a.StartTime, a.EndTime, a.Status);
}

/// <summary>
/// TASK-22: booking with real anti-double-booking under concurrency.
///
/// Two concurrent requests for the same vet's same slot are serialized by
/// a Postgres advisory lock scoped to the whole transaction
/// (`pg_advisory_xact_lock`, released automatically on commit/rollback —
/// no separate unlock call needed, and it survives connection pooling
/// correctly because it's tied to the transaction, not the session). The
/// second request to reach the lock blocks until the first commits or
/// rolls back, then re-runs its own overlap check and sees the first
/// request's now-committed row — this is what makes it a real fix for
/// the race, not just a naked "check then insert" that both requests
/// could pass simultaneously.
/// </summary>
[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController(FurinaDbContext db, ITenantContext tenantContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AppointmentResponse>> Create(CreateAppointmentRequest request, CancellationToken ct)
    {
        var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == request.PetId, ct);
        if (pet is null) return NotFound(new { error = "pet_not_found" });
        if (!IsStaff() && pet.OwnerId != CurrentUserId()) return Forbid();

        var clinicExists = await db.Clinics.AnyAsync(c => c.Id == request.ClinicId, ct);
        if (!clinicExists) return NotFound(new { error = "clinic_not_found" });

        // AC-3: end_time is derived from the service's duration — the
        // clinic's own override if it has one, otherwise the catalog
        // default (TASK-17's fallback rule, reused as-is here).
        var service = await db.ServiceCatalog.FirstOrDefaultAsync(s => s.Id == request.ServiceCatalogId, ct);
        if (service is null) return NotFound(new { error = "service_not_found" });

        var overridePrice = await db.ClinicServicePrices
            .FirstOrDefaultAsync(p => p.ClinicId == request.ClinicId && p.ServiceCatalogId == request.ServiceCatalogId, ct);
        var durationMinutes = overridePrice?.DurationMinutes ?? service.DefaultDurationMinutes;
        var endTime = request.StartTime.AddMinutes(durationMinutes);

        // The lock key is the resource actually being double-booked: the
        // vet if one's specified, otherwise the clinic (no vet assigned
        // yet means the clinic's own slot capacity is what's contended).
        var lockKey = request.VetUserId?.ToString() ?? $"clinic:{request.ClinicId}";

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // hashtext() returns int4; pg_advisory_xact_lock's single-arg
        // overload wants bigint, so this needs an explicit cast — found by
        // actually running it (Postgres error 42883, "no function
        // matches"), not by reading the docs signature carefully enough.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({lockKey})::bigint)", ct);

        var overlapQuery = db.Appointments.Where(a =>
            a.Status != "Cancelled" &&
            request.StartTime < a.EndTime && endTime > a.StartTime);
        overlapQuery = request.VetUserId is { } vetId
            ? overlapQuery.Where(a => a.VetUserId == vetId)
            : overlapQuery.Where(a => a.ClinicId == request.ClinicId);

        var conflict = await overlapQuery.FirstOrDefaultAsync(ct);
        if (conflict is not null)
        {
            await transaction.RollbackAsync(ct);
            return Conflict(new
            {
                error = "slot_unavailable",
                detail = $"Khung giờ {request.StartTime:HH:mm} - {endTime:HH:mm} đã kín.",
                conflictingAppointmentId = conflict.Id,
            });
        }

        var appointment = new Appointment
        {
            TenantId = tenantContext.TenantId!.Value,
            ClinicId = request.ClinicId,
            PetId = request.PetId,
            ServiceCatalogId = request.ServiceCatalogId,
            VetUserId = request.VetUserId,
            StartTime = request.StartTime,
            EndTime = endTime,
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return AppointmentResponse.From(appointment);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentResponse>> Get(Guid id, CancellationToken ct)
    {
        var appointment = await db.Appointments.Include(a => a.Pet).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (appointment is null) return NotFound();
        if (!IsStaff() && appointment.Pet.OwnerId != CurrentUserId()) return Forbid();

        return AppointmentResponse.From(appointment);
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private bool IsStaff() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).Any(r => r != RoleNames.Customer);
}
