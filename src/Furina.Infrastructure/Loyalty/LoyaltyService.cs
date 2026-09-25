using Furina.Domain.Entities;
using Furina.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Furina.Infrastructure.Loyalty;

/// <summary>
/// TASK-29: shared points math between invoice creation (earn) and
/// invoice cancellation (refund) so both apply the exact same rounding
/// rule and both go through the same ledger-then-balance sequencing.
/// </summary>
public static class LoyaltyService
{
    /// <summary>AC-1: floor division — a partial unit (e.g. 5,000₫ short of the next 10,000₫ step) earns nothing extra.</summary>
    public static int CalculatePointsEarned(decimal totalAmount, int pointsPerUnit, decimal amountUnit) =>
        amountUnit <= 0 ? 0 : (int)(Math.Floor(totalAmount / amountUnit) * pointsPerUnit);

    public static async Task<LoyaltyAccount> GetOrCreateAccountAsync(FurinaDbContext db, Guid tenantId, Guid ownerId, CancellationToken ct)
    {
        var account = await db.LoyaltyAccounts.FirstOrDefaultAsync(a => a.OwnerId == ownerId, ct);
        if (account is null)
        {
            account = new LoyaltyAccount { TenantId = tenantId, OwnerId = ownerId, PointsBalance = 0 };
            db.LoyaltyAccounts.Add(account);
        }
        return account;
    }

    public static void ApplyEarn(FurinaDbContext db, LoyaltyAccount account, int points, Guid invoiceId)
    {
        if (points <= 0) return;
        account.PointsBalance += points;
        db.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            TenantId = account.TenantId,
            LoyaltyAccountId = account.Id,
            Type = LoyaltyTransactionTypes.Earn,
            Points = points,
            InvoiceId = invoiceId,
        });
    }

    /// <summary>AC-2 (TASK-29 test case 3): reverses exactly the points a specific invoice earned, logging a Refund entry, not a second Earn.</summary>
    public static async Task RefundForInvoiceAsync(FurinaDbContext db, Guid invoiceId, CancellationToken ct)
    {
        var earnTx = await db.LoyaltyTransactions
            .FirstOrDefaultAsync(t => t.InvoiceId == invoiceId && t.Type == LoyaltyTransactionTypes.Earn, ct);
        if (earnTx is null) return;

        var account = await db.LoyaltyAccounts.FirstAsync(a => a.Id == earnTx.LoyaltyAccountId, ct);
        account.PointsBalance -= earnTx.Points;

        db.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            TenantId = account.TenantId,
            LoyaltyAccountId = account.Id,
            Type = LoyaltyTransactionTypes.Refund,
            Points = -earnTx.Points,
            InvoiceId = invoiceId,
        });
    }
}
