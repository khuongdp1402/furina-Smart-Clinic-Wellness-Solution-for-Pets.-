using System.Text;
using System.Text.Json.Serialization;
using Furina.Api.Auth;
using Furina.Api.Config;
using Furina.Infrastructure.Auth;
using Furina.Infrastructure.MultiTenancy;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// TASK-16: request/response DTOs use enums like DayOfWeek as readable
// strings ("Monday"), not the framework's numeric default — found by
// actually calling the endpoint with a day name and getting a real 400.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// --- Multi-tenant database wiring (TASK-11) ---
// Scoped so each HTTP request gets its own tenant value and its own
// connection-interceptor instance stamping that tenant onto the session.
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<TenantConnectionInterceptor>();

builder.Services.AddDbContext<FurinaDbContext>((sp, options) =>
{
    var connectionString = builder.Configuration.GetConnectionString("Furina")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:Furina configuration.");
    options.UseNpgsql(connectionString);
    options.AddInterceptors(sp.GetRequiredService<TenantConnectionInterceptor>());
});

// --- Auth (TASK-12) ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<MedicalRecordOptions>(builder.Configuration.GetSection(MedicalRecordOptions.SectionName));
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing Jwt configuration section.");
if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
{
    throw new InvalidOperationException("Jwt:Secret must be configured (see appsettings.Development.json).");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Without this, JwtBearerHandler silently remaps standard short
        // claim names (e.g. "sub") to legacy XML-SOAP claim URIs on the
        // way in — found for real when PetsController's
        // FindFirstValue(JwtRegisteredClaimNames.Sub) came back null even
        // though the token (inspected via jwt.io) clearly had a "sub"
        // claim. Every claim now reads back exactly as JwtTokenService
        // wrote it.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization(options => options.AddFurinaPolicies());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Tenant must be resolved before anything else touches the DB or checks a
// JWT's tenant_id claim (TASK-12 AC-2).
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();

// Rejects a token whose tenant_id claim doesn't match the resolved tenant,
// before UseAuthorization evaluates role policies.
app.UseMiddleware<TenantClaimGuardMiddleware>();

app.UseAuthorization();

app.MapControllers();

if (app.Configuration.GetValue<bool>("Seed:RunOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FurinaDbContext>();
    var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
    await SeedData.SeedAsync(db, tenantContext);
}

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
