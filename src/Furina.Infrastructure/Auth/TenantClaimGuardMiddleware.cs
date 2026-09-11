using Furina.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace Furina.Infrastructure.Auth;

/// <summary>
/// Extra guard beyond TASK-12's literal AC-2: a valid JWT issued for tenant
/// A must not work against tenant B's subdomain/header just because the
/// signature checks out. Runs after UseAuthentication (so User.Claims is
/// populated) and before UseAuthorization/controllers.
/// </summary>
public class TenantClaimGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var identity = context.User.Identity;
        if (identity is { IsAuthenticated: true })
        {
            var tokenTenantId = context.User.FindFirst(JwtTokenService.TenantIdClaimType)?.Value;
            if (tokenTenantId is null
                || !Guid.TryParse(tokenTenantId, out var parsed)
                || parsed != tenantContext.TenantId)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "tenant_mismatch",
                    detail = "Token does not belong to the resolved tenant.",
                });
                return;
            }
        }

        await next(context);
    }
}
