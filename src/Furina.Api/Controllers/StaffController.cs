using Furina.Api.Auth;
using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record AssignStaffRequest(Guid UserId, string JobTitle);

public record StaffResponse(Guid Id, Guid UserId, string UserEmail, Guid ClinicId, string JobTitle)
{
    public static StaffResponse From(Staff s) => new(s.Id, s.UserId, s.User.Email, s.ClinicId, s.JobTitle);
}

public record ShiftRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public record ShiftResponse(Guid Id, Guid StaffId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime)
{
    public static ShiftResponse From(Shift s) => new(s.Id, s.StaffId, s.DayOfWeek, s.StartTime, s.EndTime);
}

/// <summary>
/// TASK-16: staff assignment to clinics + recurring weekly shifts.
/// Read is open to any authenticated role; writes need
/// <see cref="Policies.ClinicManage"/> (Owner or Receptionist).
/// </summary>
[ApiController]
[Authorize]
public class StaffController(FurinaDbContext db, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("api/clinics/{clinicId:guid}/staff")]
    public async Task<ActionResult<List<StaffResponse>>> ListForClinic(Guid clinicId, CancellationToken ct)
    {
        var staff = await db.Staff.Include(s => s.User)
            .Where(s => s.ClinicId == clinicId)
            .ToListAsync(ct);
        return staff.Select(StaffResponse.From).ToList();
    }

    [HttpPost("api/clinics/{clinicId:guid}/staff")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<StaffResponse>> Assign(Guid clinicId, AssignStaffRequest request, CancellationToken ct)
    {
        var clinicExists = await db.Clinics.AnyAsync(c => c.Id == clinicId, ct);
        if (!clinicExists) return NotFound(new { error = "clinic_not_found" });

        var alreadyAssigned = await db.Staff.AnyAsync(s => s.ClinicId == clinicId && s.UserId == request.UserId, ct);
        if (alreadyAssigned) return Conflict(new { error = "already_assigned" });

        var staff = new Staff
        {
            TenantId = tenantContext.TenantId!.Value,
            ClinicId = clinicId,
            UserId = request.UserId,
            JobTitle = request.JobTitle,
        };
        db.Staff.Add(staff);
        await db.SaveChangesAsync(ct);

        await db.Entry(staff).Reference(s => s.User).LoadAsync(ct);
        return StaffResponse.From(staff);
    }

    /// <summary>
    /// TASK-16 test case 3: unassigning staff who still has shifts must
    /// warn first, never delete silently. `confirm=true` proceeds and
    /// cascades the shift deletion (a person no longer staffed at a
    /// clinic can't keep shifts there).
    /// </summary>
    [HttpDelete("api/staff/{staffId:guid}")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<IActionResult> Unassign(Guid staffId, [FromQuery] bool confirm, CancellationToken ct)
    {
        var staff = await db.Staff.FirstOrDefaultAsync(s => s.Id == staffId, ct);
        if (staff is null) return NotFound();

        var shifts = await db.Shifts.Where(sh => sh.StaffId == staffId).ToListAsync(ct);
        if (shifts.Count > 0 && !confirm)
        {
            return Conflict(new
            {
                error = "staff_has_shifts",
                detail = "Staff still has shifts assigned. Pass ?confirm=true to unassign and remove them.",
                shifts = shifts.Select(ShiftResponse.From),
            });
        }

        db.Staff.Remove(staff); // cascades shifts via FK ON DELETE CASCADE
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("api/clinics/{clinicId:guid}/shifts")]
    public async Task<ActionResult<List<ShiftResponse>>> WeeklySchedule(Guid clinicId, CancellationToken ct)
    {
        var shifts = await db.Shifts
            .Where(sh => sh.Staff.ClinicId == clinicId)
            .OrderBy(sh => sh.DayOfWeek).ThenBy(sh => sh.StartTime)
            .ToListAsync(ct);
        return shifts.Select(ShiftResponse.From).ToList();
    }

    [HttpPost("api/staff/{staffId:guid}/shifts")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<ShiftResponse>> AddShift(Guid staffId, ShiftRequest request, CancellationToken ct)
    {
        var staff = await db.Staff.FirstOrDefaultAsync(s => s.Id == staffId, ct);
        if (staff is null) return NotFound(new { error = "staff_not_found" });

        if (request.EndTime <= request.StartTime)
        {
            return BadRequest(new { error = "Giờ kết thúc ca phải sau giờ bắt đầu." });
        }

        // TASK-16 AC-2: overlap is checked across ALL clinics this person
        // works at, not just the one being scheduled here — the same
        // person can't be in two shifts at once regardless of location.
        var thisPersonsShifts = await db.Shifts
            .Where(sh => sh.Staff.UserId == staff.UserId && sh.DayOfWeek == request.DayOfWeek)
            .Select(sh => new { sh.Id, sh.StartTime, sh.EndTime, sh.StaffId })
            .ToListAsync(ct);

        var conflict = thisPersonsShifts.FirstOrDefault(sh =>
            request.StartTime < sh.EndTime && request.EndTime > sh.StartTime);
        if (conflict is not null)
        {
            return Conflict(new
            {
                error = "shift_overlap",
                detail = $"Trùng ca đã có: {request.DayOfWeek} {conflict.StartTime}-{conflict.EndTime}.",
                conflictingShiftId = conflict.Id,
            });
        }

        var shift = new Shift
        {
            TenantId = tenantContext.TenantId!.Value,
            StaffId = staffId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
        };
        db.Shifts.Add(shift);
        await db.SaveChangesAsync(ct);

        return ShiftResponse.From(shift);
    }

    [HttpDelete("api/staff/{staffId:guid}/shifts/{shiftId:guid}")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<IActionResult> RemoveShift(Guid staffId, Guid shiftId, CancellationToken ct)
    {
        var shift = await db.Shifts.FirstOrDefaultAsync(sh => sh.Id == shiftId && sh.StaffId == staffId, ct);
        if (shift is null) return NotFound();

        db.Shifts.Remove(shift);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
