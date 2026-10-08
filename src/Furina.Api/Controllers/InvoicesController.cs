using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Furina.Api.Auth;
using Furina.Domain.Entities;
using Furina.Infrastructure.Inventory;
using Furina.Infrastructure.Loyalty;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record InvoiceLineRequest(string Type, Guid? ServiceCatalogId, Guid? InventoryItemId, int Quantity);

/// <summary>
/// TASK-27 AC-3: TotalAmount here is accepted in the request shape for
/// client-side display convenience, but the server NEVER reads it — it's
/// recomputed from real prices every time. Keeping the field (rather than
/// omitting it) is deliberate: it lets the test simulate a client sending
/// a fraudulent total and prove the server ignores it, instead of the
/// field simply not existing to send in the first place.
/// </summary>
public record CreateInvoiceRequest(Guid ClinicId, Guid? VisitId, Guid? OwnerId, List<InvoiceLineRequest> Lines, decimal? TotalAmount);

public record InvoiceLineResponse(string Type, Guid? ServiceCatalogId, Guid? InventoryItemId, int Quantity, decimal UnitPrice, decimal LineTotal)
{
    public static InvoiceLineResponse From(InvoiceLineItem l) => new(l.Type, l.ServiceCatalogId, l.InventoryItemId, l.Quantity, l.UnitPrice, l.LineTotal);
}

public record InvoiceResponse(Guid Id, Guid ClinicId, Guid? VisitId, Guid? OwnerId, decimal TotalAmount, string Status, DateTimeOffset CreatedAt, List<InvoiceLineResponse> Lines)
{
    public static InvoiceResponse From(Invoice i) => new(
        i.Id, i.ClinicId, i.VisitId, i.OwnerId, i.TotalAmount, i.Status, i.CreatedAt, i.Lines.Select(InvoiceLineResponse.From).ToList());
}

/// <summary>
/// TASK-27: POS invoicing. Creating an invoice and deducting the stock
/// its inventory-item lines consume happen in ONE transaction — if any
/// line can't be fulfilled (insufficient stock), the whole invoice is
/// rolled back, not just that line, so there's never a partially-billed,
/// partially-stocked "orphan" invoice.
/// </summary>
[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController(FurinaDbContext db, ITenantContext tenantContext) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<InvoiceResponse>> Create(CreateInvoiceRequest request, CancellationToken ct)
    {
        var clinicExists = await db.Clinics.AnyAsync(c => c.Id == request.ClinicId, ct);
        if (!clinicExists) return NotFound(new { error = "clinic_not_found" });

        if (request.Lines.Count == 0) return BadRequest(new { error = "invoice_needs_at_least_one_line" });

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var lineItems = new List<InvoiceLineItem>();
        try
        {
            foreach (var line in request.Lines)
            {
                InvoiceLineItem lineItem;

                if (line.Type == InvoiceLineType.Service)
                {
                    if (line.ServiceCatalogId is not { } serviceId)
                        return BadRequest(new { error = "service_catalog_id_required" });

                    var service = await db.ServiceCatalog.FirstOrDefaultAsync(s => s.Id == serviceId, ct);
                    if (service is null) return NotFound(new { error = "service_not_found" });

                    // AC-1/AC-3: real unit price — the clinic's own
                    // override if it has one (TASK-17's fallback rule),
                    // never anything the client claims the price is.
                    var overridePrice = await db.ClinicServicePrices
                        .FirstOrDefaultAsync(p => p.ClinicId == request.ClinicId && p.ServiceCatalogId == serviceId, ct);
                    var unitPrice = overridePrice?.Price ?? service.DefaultPrice;

                    lineItem = new InvoiceLineItem
                    {
                        TenantId = tenantContext.TenantId!.Value,
                        Type = InvoiceLineType.Service,
                        ServiceCatalogId = serviceId,
                        Quantity = line.Quantity,
                        UnitPrice = unitPrice,
                        LineTotal = unitPrice * line.Quantity,
                    };
                }
                else if (line.Type == InvoiceLineType.InventoryItem)
                {
                    if (line.InventoryItemId is not { } itemId)
                        return BadRequest(new { error = "inventory_item_id_required" });

                    var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == itemId, ct);
                    if (item is null) return NotFound(new { error = "inventory_item_not_found" });

                    // AC-2: locked + deducted INSIDE this same transaction.
                    // If this throws, the catch block below rolls back
                    // everything already added to `lineItems` too — no
                    // invoice, no line items, no partial stock deduction.
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"SELECT pg_advisory_xact_lock(hashtext({itemId.ToString()})::bigint)", ct);
                    await InventoryFefoService.IssueAsync(db, itemId, line.Quantity, ct);

                    lineItem = new InvoiceLineItem
                    {
                        TenantId = tenantContext.TenantId!.Value,
                        Type = InvoiceLineType.InventoryItem,
                        InventoryItemId = itemId,
                        Quantity = line.Quantity,
                        UnitPrice = item.Price,
                        LineTotal = item.Price * line.Quantity,
                    };
                }
                else
                {
                    return BadRequest(new { error = "invalid_line_type", detail = $"Unknown type '{line.Type}'." });
                }

                lineItems.Add(lineItem);
            }
        }
        catch (InsufficientStockException ex)
        {
            await transaction.RollbackAsync(ct);
            return BadRequest(new
            {
                error = "insufficient_stock",
                detail = $"Không đủ tồn kho cho mặt hàng {ex.InventoryItemId} (yêu cầu {ex.Requested}, còn {ex.Available}).",
                inventoryItemId = ex.InventoryItemId,
            });
        }

        // AC-1/AC-3: the total is always the sum of server-computed line
        // totals — request.TotalAmount is read nowhere above and is
        // never referenced here either.
        var totalAmount = lineItems.Sum(l => l.LineTotal);
        var invoice = new Invoice
        {
            TenantId = tenantContext.TenantId!.Value,
            ClinicId = request.ClinicId,
            VisitId = request.VisitId,
            OwnerId = request.OwnerId,
            CreatedByUserId = CurrentUserId(),
            TotalAmount = totalAmount,
            Lines = lineItems,
        };
        db.Invoices.Add(invoice);

        // TASK-29 AC-1: only a paying customer (OwnerId set) earns points,
        // and always from the server-computed total, never the client's.
        if (request.OwnerId is { } ownerId)
        {
            var tenant = await db.Tenants.FirstAsync(t => t.Id == invoice.TenantId, ct);
            var points = LoyaltyService.CalculatePointsEarned(totalAmount, tenant.LoyaltyPointsPerUnit, tenant.LoyaltyPointsAmountUnit);
            var account = await LoyaltyService.GetOrCreateAccountAsync(db, invoice.TenantId, ownerId, ct);
            LoyaltyService.ApplyEarn(db, account, points, invoice.Id);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return InvoiceResponse.From(invoice);
    }

    /// <summary>TASK-29 AC-2 (test case 3): cancelling an invoice that earned points refunds exactly those points, logging a Refund entry.</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.ClinicManage)]
    public async Task<ActionResult<InvoiceResponse>> Cancel(Guid id, CancellationToken ct)
    {
        var invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null) return NotFound();
        if (invoice.Status == InvoiceStatuses.Cancelled) return BadRequest(new { error = "invoice_already_cancelled" });

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        invoice.Status = InvoiceStatuses.Cancelled;
        await LoyaltyService.RefundForInvoiceAsync(db, invoice.Id, ct);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return InvoiceResponse.From(invoice);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceResponse>> Get(Guid id, CancellationToken ct)
    {
        var invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null) return NotFound();
        return InvoiceResponse.From(invoice);
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
}
