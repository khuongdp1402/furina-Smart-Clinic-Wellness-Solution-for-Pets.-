using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Furina.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add/update` construct the DbContext at design
/// time without spinning up the whole app. Connection string is read from
/// the FURINA_DB_CONNECTION env var, falling back to the local docker-compose
/// default (app role — NOT superuser, so RLS actually applies here too).
/// </summary>
public class FurinaDbContextFactory : IDesignTimeDbContextFactory<FurinaDbContext>
{
    public FurinaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FURINA_DB_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=furina;Username=furina_app;Password=furina_app_dev_pw";

        var optionsBuilder = new DbContextOptionsBuilder<FurinaDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new FurinaDbContext(optionsBuilder.Options);
    }
}
