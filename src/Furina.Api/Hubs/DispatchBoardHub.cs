using Furina.Domain.Entities;
using Furina.Infrastructure.Auth;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Hubs;

/// <summary>
/// TASK-25: real-time dispatch board. One SignalR group per clinic
/// ("clinic:{clinicId}") — joining a clinic outside the caller's own
/// tenant is rejected (AC-2: no event leakage between clinics/tenants;
/// SignalR groups aren't secret, so this is checked explicitly rather
/// than relying on the group name being hard to guess).
///
/// Single-server, in-memory SignalR (no Redis backplane) — this task's
/// ACs only need one clinic's viewers to see each other's updates, which
/// works without one; add a backplane if/when the API ever scales past a
/// single instance.
/// </summary>
[Authorize]
public class DispatchBoardHub(FurinaDbContext db, ITenantContext tenantContext) : Hub
{
    public static string GroupName(Guid clinicId) => $"clinic:{clinicId}";

    /// <summary>
    /// SignalR gives EVERY hub method invocation its own fresh DI scope
    /// (visible in the stack trace as `AsyncServiceScope scope` inside
    /// DefaultHubDispatcher) — not one scope for the whole connection, and
    /// definitely not the same scope as the HTTP request that carried the
    /// invocation (negotiate/poll/send are each their own request under
    /// long polling). Found for real: setting the tenant once in
    /// OnConnectedAsync had zero effect on the next call's ITenantContext
    /// — it's a brand new instance every time. The only thing that DOES
    /// persist across calls on one connection is `Context.User` (the
    /// authenticated principal), so every method that touches the DB
    /// re-derives the tenant from it, fresh, every single call.
    /// </summary>
    private void SetTenantFromConnection()
    {
        var tenantIdClaim = Context.User?.FindFirst(JwtTokenService.TenantIdClaimType)?.Value;
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            throw new HubException("missing_tenant_claim");
        }
        tenantContext.SetTenant(tenantId);
    }

    public async Task JoinClinic(Guid clinicId)
    {
        SetTenantFromConnection();

        var clinicExists = await db.Clinics.AnyAsync(c => c.Id == clinicId);
        if (!clinicExists)
        {
            // Either the clinic doesn't exist, or (thanks to RLS) it
            // belongs to another tenant and is invisible to this
            // connection's session — both cases get the same refusal, so
            // this can't be used to probe whether a clinic id from
            // another tenant exists.
            throw new HubException("clinic_not_found_or_not_in_tenant");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(clinicId));
    }

    public Task LeaveClinic(Guid clinicId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(clinicId));

    /// <summary>
    /// TASK-25 test case 3: after a dropped connection reconnects, the
    /// client calls this once (having re-joined the group) to pull the
    /// authoritative current queue rather than trusting whatever it had
    /// cached from before the drop.
    /// </summary>
    public async Task<List<AppointmentQueueItem>> GetCurrentQueue(Guid clinicId)
    {
        SetTenantFromConnection();

        var todayStart = DateTimeOffset.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var appointments = await db.Appointments
            .Where(a => a.ClinicId == clinicId && a.StartTime >= todayStart && a.StartTime < todayEnd)
            .OrderBy(a => a.StartTime)
            .Select(a => new AppointmentQueueItem(a.Id, a.PetId, a.StartTime, a.EndTime, a.Status))
            .ToListAsync();

        return appointments;
    }
}

public record AppointmentQueueItem(Guid AppointmentId, Guid PetId, DateTimeOffset StartTime, DateTimeOffset EndTime, string Status);
