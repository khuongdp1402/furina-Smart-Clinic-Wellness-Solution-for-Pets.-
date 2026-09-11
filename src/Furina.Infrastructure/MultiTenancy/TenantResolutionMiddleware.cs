using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Furina.Infrastructure.MultiTenancy;

/// <summary>
/// TASK-12 AC-2: "request không kèm tenant -> 400 trước khi chạm business
/// logic". Runs before authentication/authorization and before any
/// controller, so a request that can't be pinned to a tenant never reaches
/// a DbContext query (which would otherwise just see zero rows thanks to
/// RLS — a confusing 404/empty-list instead of an honest 400).
///
/// Tenant is read from the subdomain in production (`acme.furina.app` ->
/// slug "acme") or the `X-Tenant-Id` header in dev/test, matching TASK-12's
/// spec. `tenants` has no RLS (see TASK-11), so this lookup works even
/// before any tenant context is set.
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string TenantHeaderName = "X-Tenant-Id";

    // Paths that don't belong to any tenant (dev tooling, health checks).
    private static readonly string[] ExemptPathPrefixes = ["/openapi", "/swagger", "/health"];

    public async Task InvokeAsync(HttpContext context, FurinaDbContext db, ITenantContext tenantContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (ExemptPathPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var slug = ResolveTenantSlug(context);
        if (string.IsNullOrWhiteSpace(slug))
        {
            await WriteBadRequest(context, "missing_tenant",
                $"Request must identify a tenant via subdomain or the {TenantHeaderName} header.");
            return;
        }

        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug && t.IsActive, context.RequestAborted);

        if (tenant is null)
        {
            await WriteBadRequest(context, "unknown_tenant", $"No active tenant found for '{slug}'.");
            return;
        }

        tenantContext.SetTenant(tenant.Id);
        context.Items[nameof(ITenantContext.TenantId)] = tenant.Id;

        await next(context);
    }

    private static string? ResolveTenantSlug(HttpContext context)
    {
        var host = context.Request.Host.Host;
        var labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries);

        // "acme.furina.app" (3+ labels) -> subdomain is the tenant slug.
        // "localhost" / "127.0.0.1" / bare "furina.app" -> no usable
        // subdomain, fall back to the dev/test header.
        if (labels.Length >= 3 && !IsIpAddress(host))
        {
            return labels[0];
        }

        return context.Request.Headers[TenantHeaderName].FirstOrDefault();
    }

    private static bool IsIpAddress(string host) => System.Net.IPAddress.TryParse(host, out _);

    private static Task WriteBadRequest(HttpContext context, string code, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsJsonAsync(new { error = code, detail });
    }
}
