using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Furina.Api.Auth;
using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Notifications;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record VaccinationRequest(string VaccineName, DateOnly DateGiven, DateOnly NextDueDate);

public record VaccinationResponse(Guid Id, Guid PetId, string VaccineName, DateOnly DateGiven, DateOnly NextDueDate)
{
    public static VaccinationResponse From(VaccinationRecord v) => new(v.Id, v.PetId, v.VaccineName, v.DateGiven, v.NextDueDate);
}

public record NotificationLogResponse(Guid Id, Guid VaccinationRecordId, int MilestoneDay, DateTimeOffset SentAt)
{
    public static NotificationLogResponse From(NotificationLog n) => new(n.Id, n.VaccinationRecordId, n.MilestoneDay, n.SentAt);
}

/// <summary>TASK-20: vaccination history + the reminder job's manual trigger (for ops and for testing).</summary>
[ApiController]
[Authorize]
public class VaccinationsController(FurinaDbContext db, ITenantContext tenantContext, VaccinationReminderJob reminderJob) : ControllerBase
{
    [HttpGet("api/pets/{petId:guid}/vaccinations")]
    public async Task<ActionResult<List<VaccinationResponse>>> List(Guid petId, CancellationToken ct)
    {
        var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == petId, ct);
        if (pet is null) return NotFound();
        if (!IsStaff() && pet.OwnerId != CurrentUserId()) return Forbid();

        var records = await db.VaccinationRecords
            .Where(v => v.PetId == petId)
            .OrderByDescending(v => v.DateGiven)
            .ToListAsync(ct);
        return records.Select(VaccinationResponse.From).ToList();
    }

    [HttpPost("api/pets/{petId:guid}/vaccinations")]
    [Authorize(Policy = Policies.VetOnly)]
    public async Task<ActionResult<VaccinationResponse>> Create(Guid petId, VaccinationRequest request, CancellationToken ct)
    {
        var petExists = await db.Pets.AnyAsync(p => p.Id == petId, ct);
        if (!petExists) return NotFound(new { error = "pet_not_found" });

        var record = new VaccinationRecord
        {
            TenantId = tenantContext.TenantId!.Value,
            PetId = petId,
            VaccineName = request.VaccineName,
            DateGiven = request.DateGiven,
            NextDueDate = request.NextDueDate,
            CreatedByUserId = CurrentUserId(),
        };
        db.VaccinationRecords.Add(record);
        await db.SaveChangesAsync(ct);

        return VaccinationResponse.From(record);
    }

    [HttpGet("api/vaccination-records/{id:guid}/notifications")]
    public async Task<ActionResult<List<NotificationLogResponse>>> NotificationsFor(Guid id, CancellationToken ct)
    {
        var logs = await db.NotificationLogs
            .Where(n => n.VaccinationRecordId == id)
            .OrderBy(n => n.MilestoneDay)
            .ToListAsync(ct);
        return logs.Select(NotificationLogResponse.From).ToList();
    }

    /// <summary>
    /// Manual trigger for the daily reminder job — used by SuperAdmin for
    /// ops, and by TASK-20's own tests to prove AC-2 (call this twice in
    /// the same day, second call creates 0 new notifications) without
    /// waiting for the real 08:00 run.
    /// </summary>
    [HttpPost("api/admin/vaccination-reminders/run")]
    [Authorize(Policy = Policies.OwnerOnly)]
    public async Task<ActionResult> RunReminderJob(CancellationToken ct)
    {
        var created = await reminderJob.RunAsync(ct);
        return Ok(new { notificationsCreated = created });
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private bool IsStaff() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).Any(r => r != RoleNames.Customer);
}
