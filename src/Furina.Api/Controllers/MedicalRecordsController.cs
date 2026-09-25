using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Furina.Api.Auth;
using Furina.Api.Config;
using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Furina.Api.Controllers;

public record CreateVisitRequest(Guid PetId, Guid ClinicId);

public record VisitResponse(Guid Id, Guid PetId, Guid ClinicId, Guid VetUserId, DateTimeOffset VisitDate)
{
    public static VisitResponse From(Visit v) => new(v.Id, v.PetId, v.ClinicId, v.VetUserId, v.VisitDate);
}

public record SoapNoteRequest(string Subjective, string Objective, string Assessment, string Plan);

public record MedicalRecordResponse(
    Guid Id, Guid VisitId, Guid PetId, Guid VetUserId,
    string Subjective, string Objective, string Assessment, string Plan,
    DateTimeOffset CreatedAt, bool IsLocked)
{
    public static MedicalRecordResponse From(MedicalRecord r, bool isLocked) => new(
        r.Id, r.VisitId, r.PetId, r.VetUserId, r.Subjective, r.Objective, r.Assessment, r.Plan, r.CreatedAt, isLocked);
}

public record AuditLogResponse(Guid Id, Guid ChangedByUserId, DateTimeOffset ChangedAt,
    string PreviousSubjective, string PreviousObjective, string PreviousAssessment, string PreviousPlan)
{
    public static AuditLogResponse From(MedicalRecordAuditLog a) => new(
        a.Id, a.ChangedByUserId, a.ChangedAt, a.PreviousSubjective, a.PreviousObjective, a.PreviousAssessment, a.PreviousPlan);
}

/// <summary>
/// TASK-19: SOAP-note medical records, one per <see cref="Visit"/>, edit-
/// locked after <see cref="MedicalRecordOptions.EditLockHours"/> hours.
/// Creating a visit/note needs <see cref="Policies.VetOnly"/> — a
/// receptionist can check a pet in but shouldn't be writing medical
/// assessments.
/// </summary>
[ApiController]
[Authorize]
public class MedicalRecordsController(
    FurinaDbContext db, ITenantContext tenantContext, IOptions<MedicalRecordOptions> options) : ControllerBase
{
    private readonly MedicalRecordOptions _options = options.Value;

    [HttpPost("api/visits")]
    [Authorize(Policy = Policies.VetOnly)]
    public async Task<ActionResult<VisitResponse>> CreateVisit(CreateVisitRequest request, CancellationToken ct)
    {
        var petExists = await db.Pets.AnyAsync(p => p.Id == request.PetId, ct);
        if (!petExists) return NotFound(new { error = "pet_not_found" });

        var visit = new Visit
        {
            TenantId = tenantContext.TenantId!.Value,
            PetId = request.PetId,
            ClinicId = request.ClinicId,
            VetUserId = CurrentUserId(),
        };
        db.Visits.Add(visit);
        await db.SaveChangesAsync(ct);

        return VisitResponse.From(visit);
    }

    [HttpPost("api/visits/{visitId:guid}/medical-record")]
    [Authorize(Policy = Policies.VetOnly)]
    public async Task<ActionResult<MedicalRecordResponse>> CreateSoapNote(Guid visitId, SoapNoteRequest request, CancellationToken ct)
    {
        var visit = await db.Visits.FirstOrDefaultAsync(v => v.Id == visitId, ct);
        if (visit is null) return NotFound(new { error = "visit_not_found" });

        var record = new MedicalRecord
        {
            TenantId = tenantContext.TenantId!.Value,
            VisitId = visitId,
            PetId = visit.PetId,
            VetUserId = CurrentUserId(),
            Subjective = request.Subjective,
            Objective = request.Objective,
            Assessment = request.Assessment,
            Plan = request.Plan,
        };
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync(ct);

        return MedicalRecordResponse.From(record, isLocked: false);
    }

    [HttpGet("api/medical-records/{id:guid}")]
    public async Task<ActionResult<MedicalRecordResponse>> Get(Guid id, CancellationToken ct)
    {
        var record = await db.MedicalRecords.Include(r => r.Pet).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) return NotFound();
        if (!IsStaff() && record.Pet.OwnerId != CurrentUserId()) return Forbid();

        return MedicalRecordResponse.From(record, IsLocked(record));
    }

    /// <summary>
    /// TASK-19 AC-2/AC-3: past the lock window, this is a real 403; inside
    /// it, the edit is saved AND a <see cref="MedicalRecordAuditLog"/> row
    /// captures what the fields were right before this change.
    /// </summary>
    [HttpPut("api/medical-records/{id:guid}")]
    [Authorize(Policy = Policies.VetOnly)]
    public async Task<ActionResult<MedicalRecordResponse>> Update(Guid id, SoapNoteRequest request, CancellationToken ct)
    {
        var record = await db.MedicalRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) return NotFound();

        if (IsLocked(record))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "medical_record_locked",
                detail = $"Edit window ({_options.EditLockHours}h from creation) has passed.",
            });
        }

        db.MedicalRecordAuditLogs.Add(new MedicalRecordAuditLog
        {
            TenantId = tenantContext.TenantId!.Value,
            MedicalRecordId = record.Id,
            ChangedByUserId = CurrentUserId(),
            PreviousSubjective = record.Subjective,
            PreviousObjective = record.Objective,
            PreviousAssessment = record.Assessment,
            PreviousPlan = record.Plan,
        });

        record.Subjective = request.Subjective;
        record.Objective = request.Objective;
        record.Assessment = request.Assessment;
        record.Plan = request.Plan;
        await db.SaveChangesAsync(ct);

        return MedicalRecordResponse.From(record, isLocked: false);
    }

    [HttpGet("api/medical-records/{id:guid}/audit-logs")]
    public async Task<ActionResult<List<AuditLogResponse>>> AuditLogs(Guid id, CancellationToken ct)
    {
        var record = await db.MedicalRecords.Include(r => r.Pet).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) return NotFound();
        if (!IsStaff() && record.Pet.OwnerId != CurrentUserId()) return Forbid();

        var logs = await db.MedicalRecordAuditLogs
            .Where(a => a.MedicalRecordId == id)
            .OrderBy(a => a.ChangedAt)
            .ToListAsync(ct);
        return logs.Select(AuditLogResponse.From).ToList();
    }

    private bool IsLocked(MedicalRecord record) =>
        DateTimeOffset.UtcNow > record.CreatedAt.AddHours(_options.EditLockHours);

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private bool IsStaff() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).Any(r => r != RoleNames.Customer);
}
