using Furina.Api.Auth;
using Furina.Domain.Entities;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record LoyaltyAccountResponse(Guid OwnerId, int PointsBalance);

public record LoyaltyTransactionResponse(Guid Id, string Type, int Points, Guid? InvoiceId, DateTimeOffset CreatedAt)
{
    public static LoyaltyTransactionResponse From(LoyaltyTransaction t) => new(t.Id, t.Type, t.Points, t.InvoiceId, t.CreatedAt);
}

public record RedeemPointsRequest(int Points);

/// <summary>TASK-29: loyalty points balance, history, and manual redemption.</summary>
[ApiController]
[Route("api/loyalty-accounts")]
[Authorize]
public class LoyaltyController(FurinaDbContext db, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("{ownerId:guid}")]
    public async Task<ActionResult<LoyaltyAccountResponse>> GetBalance(Guid ownerId, CancellationToken ct)
    {
        var account = await db.LoyaltyAccounts.FirstOrDefaultAsync(a => a.OwnerId == ownerId, ct);
        return new LoyaltyAccountResponse(ownerId, account?.PointsBalance ?? 0);
    }

    [HttpGet("{ownerId:guid}/transactions")]
    public async Task<ActionResult<List<LoyaltyTransactionResponse>>> GetHistory(Guid ownerId, CancellationToken ct)
    {
        var account = await db.LoyaltyAccounts.FirstOrDefaultAsync(a => a.OwnerId == ownerId, ct);
        if (account is null) return new List<LoyaltyTransactionResponse>();

        var transactions = await db.LoyaltyTransactions
            .Where(t => t.LoyaltyAccountId == account.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
        return transactions.Select(LoyaltyTransactionResponse.From).ToList();
    }

    /// <summary>TASK-29 test case 2: redeeming more points than the current balance is rejected — the balance never goes negative from a redemption.</summary>
    [HttpPost("{ownerId:guid}/redeem")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<LoyaltyAccountResponse>> Redeem(Guid ownerId, RedeemPointsRequest request, CancellationToken ct)
    {
        if (request.Points <= 0) return BadRequest(new { error = "points_must_be_positive" });

        var account = await db.LoyaltyAccounts.FirstOrDefaultAsync(a => a.OwnerId == ownerId, ct);
        if (account is null || account.PointsBalance < request.Points)
        {
            return BadRequest(new
            {
                error = "insufficient_points",
                available = account?.PointsBalance ?? 0,
            });
        }

        account.PointsBalance -= request.Points;
        db.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            TenantId = tenantContext.TenantId!.Value,
            LoyaltyAccountId = account.Id,
            Type = LoyaltyTransactionTypes.Redeem,
            Points = -request.Points,
        });
        await db.SaveChangesAsync(ct);

        return new LoyaltyAccountResponse(ownerId, account.PointsBalance);
    }
}
