using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Furina.Infrastructure.MultiTenancy;

/// <summary>
/// Every time a connection is opened for this DbContext scope, stamps the
/// Postgres session with `app.tenant_id` so the Row-Level Security policies
/// (`USING (tenant_id = current_setting('app.tenant_id')::uuid)`) can do
/// their job. Without this, RLS silently returns zero rows for everyone —
/// this interceptor is what actually turns tenant isolation "on" per request.
/// </summary>
public class TenantConnectionInterceptor(ITenantContext tenantContext) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyTenant(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await ApplyTenantAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void ApplyTenant(DbConnection connection)
    {
        if (tenantContext.TenantId is not { } tenantId) return;
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SET app.tenant_id = '{tenantId:D}'";
        cmd.ExecuteNonQuery();
    }

    private async Task ApplyTenantAsync(DbConnection connection, CancellationToken ct)
    {
        if (tenantContext.TenantId is not { } tenantId) return;
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SET app.tenant_id = '{tenantId:D}'";
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
