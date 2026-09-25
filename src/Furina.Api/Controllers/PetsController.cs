using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record PetRequest(
    string Name,
    string Species,
    string Breed,
    DateOnly? DateOfBirth,
    string Gender,
    string? PhotoUrl,
    string? MicrochipId);

public record PetResponse(
    Guid Id, Guid OwnerId, string Name, string Species, string Breed,
    DateOnly? DateOfBirth, string Gender, string? PhotoUrl, string? MicrochipId)
{
    public static PetResponse From(Pet p) => new(
        p.Id, p.OwnerId, p.Name, p.Species, p.Breed, p.DateOfBirth, p.Gender, p.PhotoUrl, p.MicrochipId);
}

public record WeightLogRequest(decimal WeightKg, DateTimeOffset MeasuredAt);

public record WeightLogResponse(Guid Id, decimal WeightKg, DateTimeOffset MeasuredAt)
{
    public static WeightLogResponse From(PetWeightLog w) => new(w.Id, w.WeightKg, w.MeasuredAt);
}

/// <summary>
/// TASK-18: Digital Pet ID. Authorization here is by OWNER, not just
/// tenant — a Customer role can only touch their own pets; clinic staff
/// (Owner/Vet/Receptionist/SuperAdmin) can touch any pet in the tenant
/// since they need to for actual treatment, which is outside this task's
/// ACs but would make the feature useless if blocked too.
/// </summary>
[ApiController]
[Authorize]
public class PetsController(FurinaDbContext db, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("api/pets")]
    public async Task<ActionResult<List<PetResponse>>> List(CancellationToken ct)
    {
        var query = db.Pets.AsQueryable();
        if (!IsStaff())
        {
            query = query.Where(p => p.OwnerId == CurrentUserId());
        }
        var pets = await query.ToListAsync(ct);
        return pets.Select(PetResponse.From).ToList();
    }

    [HttpGet("api/pets/{id:guid}")]
    public async Task<ActionResult<PetResponse>> Get(Guid id, CancellationToken ct)
    {
        var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pet is null) return NotFound();

        // TASK-18 AC-3: a customer viewing another customer's pet gets a
        // real 403, not a 404 that could be mistaken for "just doesn't
        // exist" — the pet DOES exist in this tenant, the caller simply
        // isn't allowed to see it.
        if (!IsStaff() && pet.OwnerId != CurrentUserId()) return Forbid();

        return PetResponse.From(pet);
    }

    [HttpPost("api/pets")]
    public async Task<ActionResult<PetResponse>> Create(PetRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Species))
        {
            return BadRequest(new { error = "species là bắt buộc." });
        }

        // A customer creates a pet for themselves; staff creating on
        // behalf of a walk-in customer is a later feature (needs a way to
        // pick which customer) — out of this task's scope.
        var pet = new Pet
        {
            TenantId = tenantContext.TenantId!.Value,
            OwnerId = CurrentUserId(),
            Name = request.Name,
            Species = request.Species,
            Breed = request.Breed,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            PhotoUrl = request.PhotoUrl,
            MicrochipId = request.MicrochipId,
        };
        db.Pets.Add(pet);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = pet.Id }, PetResponse.From(pet));
    }

    [HttpPut("api/pets/{id:guid}")]
    public async Task<ActionResult<PetResponse>> Update(Guid id, PetRequest request, CancellationToken ct)
    {
        var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pet is null) return NotFound();
        if (!IsStaff() && pet.OwnerId != CurrentUserId()) return Forbid();

        if (string.IsNullOrWhiteSpace(request.Species))
        {
            return BadRequest(new { error = "species là bắt buộc." });
        }

        pet.Name = request.Name;
        pet.Species = request.Species;
        pet.Breed = request.Breed;
        pet.DateOfBirth = request.DateOfBirth;
        pet.Gender = request.Gender;
        pet.PhotoUrl = request.PhotoUrl;
        pet.MicrochipId = request.MicrochipId;
        await db.SaveChangesAsync(ct);

        return PetResponse.From(pet);
    }

    [HttpDelete("api/pets/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pet is null) return NotFound();
        if (!IsStaff() && pet.OwnerId != CurrentUserId()) return Forbid();

        db.Pets.Remove(pet);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("api/pets/{id:guid}/weight-logs")]
    public async Task<ActionResult<List<WeightLogResponse>>> WeightHistory(Guid id, CancellationToken ct)
    {
        var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pet is null) return NotFound();
        if (!IsStaff() && pet.OwnerId != CurrentUserId()) return Forbid();

        var logs = await db.PetWeightLogs
            .Where(w => w.PetId == id)
            .OrderBy(w => w.MeasuredAt)
            .ToListAsync(ct);
        return logs.Select(WeightLogResponse.From).ToList();
    }

    [HttpPost("api/pets/{id:guid}/weight-logs")]
    public async Task<ActionResult<WeightLogResponse>> AddWeightLog(Guid id, WeightLogRequest request, CancellationToken ct)
    {
        var pet = await db.Pets.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (pet is null) return NotFound();
        if (!IsStaff() && pet.OwnerId != CurrentUserId()) return Forbid();

        var log = new PetWeightLog
        {
            TenantId = tenantContext.TenantId!.Value,
            PetId = id,
            WeightKg = request.WeightKg,
            MeasuredAt = request.MeasuredAt,
        };
        db.PetWeightLogs.Add(log);
        await db.SaveChangesAsync(ct);

        return WeightLogResponse.From(log);
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private bool IsStaff() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).Any(r => r != RoleNames.Customer);
}
