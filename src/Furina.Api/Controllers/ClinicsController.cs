using Furina.Api.Auth;
using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record OpeningHourDto(DayOfWeek DayOfWeek, bool IsClosed, TimeOnly? OpenTime, TimeOnly? CloseTime);

public record ClinicRequest(
    string Name,
    string Address,
    string Phone,
    bool Is24hEmergency,
    List<OpeningHourDto> OpeningHours);

public record ClinicResponse(
    Guid Id,
    string Name,
    string Address,
    string Phone,
    bool Is24hEmergency,
    bool IsArchived,
    List<OpeningHourDto> OpeningHours)
{
    public static ClinicResponse From(Clinic c) => new(
        c.Id, c.Name, c.Address, c.Phone, c.Is24hEmergency, c.IsArchived,
        c.OpeningHours.Select(h => new OpeningHourDto(h.DayOfWeek, h.IsClosed, h.OpenTime, h.CloseTime)).ToList());
}

/// <summary>
/// TASK-15: CRUD for a tenant's clinic locations. Read is open to any
/// authenticated role; create/update needs <see cref="Policies.ClinicManage"/>
/// (Owner or Receptionist); delete needs <see cref="Policies.OwnerOnly"/>.
/// </summary>
[ApiController]
[Route("api/clinics")]
[Authorize]
public class ClinicsController(FurinaDbContext db, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ClinicResponse>>> List(CancellationToken ct)
    {
        var clinics = await db.Clinics.Where(c => !c.IsArchived).ToListAsync(ct);
        return clinics.Select(ClinicResponse.From).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClinicResponse>> Get(Guid id, CancellationToken ct)
    {
        var clinic = await db.Clinics.FirstOrDefaultAsync(c => c.Id == id, ct);
        // RLS already guarantees a clinic from another tenant can never be
        // found by this query at all (TASK-15 AC verifies this at the DB
        // level) — a null here just means "no such id in this tenant",
        // which reads the same as "doesn't exist" and is the right 404.
        return clinic is null ? NotFound() : ClinicResponse.From(clinic);
    }

    [HttpPost]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<ClinicResponse>> Create(ClinicRequest request, CancellationToken ct)
    {
        var error = ValidateOpeningHours(request.OpeningHours);
        if (error is not null) return BadRequest(new { error });

        var clinic = new Clinic
        {
            // Without this, the row inserts with TenantId = Guid.Empty and
            // RLS's WITH CHECK (implied by USING when a policy doesn't
            // declare one separately) rejects the insert outright — caught
            // by testing this for real, not just trusting the RLS policy
            // exists (TASK-15's own AC-1 is about proving isolation with
            // real data, and this is the write-side half of that).
            TenantId = tenantContext.TenantId!.Value,
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Is24hEmergency = request.Is24hEmergency,
            OpeningHours = ToEntities(request.OpeningHours),
        };
        db.Clinics.Add(clinic);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = clinic.Id }, ClinicResponse.From(clinic));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<ClinicResponse>> Update(Guid id, ClinicRequest request, CancellationToken ct)
    {
        var clinic = await db.Clinics.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (clinic is null) return NotFound();

        var error = ValidateOpeningHours(request.OpeningHours);
        if (error is not null) return BadRequest(new { error });

        clinic.Name = request.Name;
        clinic.Address = request.Address;
        clinic.Phone = request.Phone;
        clinic.Is24hEmergency = request.Is24hEmergency;
        clinic.OpeningHours = ToEntities(request.OpeningHours);
        await db.SaveChangesAsync(ct);

        return ClinicResponse.From(clinic);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var clinic = await db.Clinics.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (clinic is null) return NotFound();

        // TASK-15 AC-3: a clinic with future appointments is never hard
        // deleted — archive it instead so the appointment history it's
        // still referenced by stays intact.
        var hasFutureAppointments = await db.Appointments
            .AnyAsync(a => a.ClinicId == id && a.ScheduledAt > DateTimeOffset.UtcNow, ct);

        if (hasFutureAppointments)
        {
            clinic.IsArchived = true;
            await db.SaveChangesAsync(ct);
            return Conflict(new
            {
                error = "clinic_has_future_appointments",
                detail = "Clinic has future appointments; archived instead of deleted.",
            });
        }

        db.Clinics.Remove(clinic);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static List<OpeningHour> ToEntities(List<OpeningHourDto> dtos) =>
        dtos.Select(d => new OpeningHour
        {
            DayOfWeek = d.DayOfWeek,
            IsClosed = d.IsClosed,
            OpenTime = d.OpenTime,
            CloseTime = d.CloseTime,
        }).ToList();

    /// <summary>
    /// TASK-15 AC-2: closing time earlier than (or equal to) opening time
    /// on the same day -> 400 naming exactly which day is wrong.
    /// </summary>
    private static string? ValidateOpeningHours(List<OpeningHourDto> hours)
    {
        foreach (var h in hours)
        {
            if (h.IsClosed) continue;

            if (h.OpenTime is null || h.CloseTime is null)
            {
                return $"{h.DayOfWeek}: phải có OpenTime và CloseTime khi không đóng cửa cả ngày.";
            }

            if (h.CloseTime <= h.OpenTime)
            {
                return $"{h.DayOfWeek}: giờ đóng cửa ({h.CloseTime}) phải sau giờ mở cửa ({h.OpenTime}).";
            }
        }

        return null;
    }
}
