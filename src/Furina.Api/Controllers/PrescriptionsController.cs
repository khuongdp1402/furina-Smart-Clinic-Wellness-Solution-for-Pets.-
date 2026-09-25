using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Furina.Api.Auth;
using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record PrescriptionItemDto(string DrugName, string Dosage, string Frequency, int DurationDays);

public record PrescriptionRequest(List<PrescriptionItemDto> Items);

public record PrescriptionResponse(Guid Id, Guid VisitId, Guid PetId, List<PrescriptionItemDto> Items, DateTimeOffset CreatedAt)
{
    public static PrescriptionResponse From(Prescription p) => new(
        p.Id, p.VisitId, p.PetId,
        p.Items.Select(i => new PrescriptionItemDto(i.DrugName, i.Dosage, i.Frequency, i.DurationDays)).ToList(),
        p.CreatedAt);
}

/// <summary>TASK-21: electronic prescriptions, one per visit.</summary>
[ApiController]
[Authorize]
public class PrescriptionsController(FurinaDbContext db, ITenantContext tenantContext) : ControllerBase
{
    [HttpPost("api/visits/{visitId:guid}/prescription")]
    [Authorize(Policy = Policies.VetOnly)]
    public async Task<ActionResult<PrescriptionResponse>> Create(Guid visitId, PrescriptionRequest request, CancellationToken ct)
    {
        // AC-3: prescribing against a visit_id that doesn't exist -> 404,
        // not a foreign-key error surfacing as a 500.
        var visit = await db.Visits.FirstOrDefaultAsync(v => v.Id == visitId, ct);
        if (visit is null) return NotFound(new { error = "visit_not_found" });

        var prescription = new Prescription
        {
            TenantId = tenantContext.TenantId!.Value,
            VisitId = visitId,
            PetId = visit.PetId,
            VetUserId = CurrentUserId(),
            Items = request.Items.Select(i => new PrescriptionItem
            {
                DrugName = i.DrugName,
                Dosage = i.Dosage,
                Frequency = i.Frequency,
                DurationDays = i.DurationDays,
            }).ToList(),
        };
        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync(ct);

        return PrescriptionResponse.From(prescription);
    }

    [HttpGet("api/visits/{visitId:guid}/prescription")]
    public async Task<ActionResult<PrescriptionResponse>> GetForVisit(Guid visitId, CancellationToken ct)
    {
        var prescription = await db.Prescriptions.Include(p => p.Pet).FirstOrDefaultAsync(p => p.VisitId == visitId, ct);
        if (prescription is null) return NotFound();
        if (!IsStaff() && prescription.Pet.OwnerId != CurrentUserId()) return Forbid();

        return PrescriptionResponse.From(prescription);
    }

    /// <summary>TASK-21 AC-2: prescription history across every visit for one pet, newest first.</summary>
    [HttpGet("api/pets/{petId:guid}/prescriptions")]
    public async Task<ActionResult<List<PrescriptionResponse>>> HistoryForPet(Guid petId, CancellationToken ct)
    {
        var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == petId, ct);
        if (pet is null) return NotFound();
        if (!IsStaff() && pet.OwnerId != CurrentUserId()) return Forbid();

        var prescriptions = await db.Prescriptions
            .Where(p => p.PetId == petId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
        return prescriptions.Select(PrescriptionResponse.From).ToList();
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private bool IsStaff() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).Any(r => r != RoleNames.Customer);
}
