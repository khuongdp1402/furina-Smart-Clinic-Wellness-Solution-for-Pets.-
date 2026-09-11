using Furina.Domain.Entities;
using Furina.Infrastructure.Auth;
using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Furina.Api.Controllers;

public record LoginRequest(string Email, string Password);

public record TokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public record RefreshRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

/// <summary>
/// TASK-12: login/refresh/logout. Every action here runs after
/// TenantResolutionMiddleware, so `db` is already scoped to the tenant
/// resolved from the subdomain/X-Tenant-Id header — a login always
/// authenticates against exactly one tenant's user table (RLS enforces
/// this even if the query below forgot to filter, though it doesn't need
/// to: TenantId is implicit).
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController(FurinaDbContext db, IJwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "invalid_credentials" });
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();
        return await IssueTokensAsync(user, roles, ct);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var hash = jwtTokenService.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        if (stored is null || stored.RevokedAt is not null || stored.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return Unauthorized(new { error = "invalid_refresh_token" });
        }

        var roles = stored.User.UserRoles.Select(ur => ur.Role.Name).ToArray();
        var (accessToken, accessExpiresAt) = jwtTokenService.CreateAccessToken(stored.User, roles);

        // Refresh token itself is not rotated — same opaque value keeps
        // working until it naturally expires or is explicitly revoked via
        // /auth/logout.
        return new TokenResponse(accessToken, accessExpiresAt, request.RefreshToken, stored.ExpiresAt);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        var hash = jwtTokenService.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

        // TASK-12 DoD: "revoke được thật sự (đăng xuất vô hiệu hoá ngay,
        // không chỉ xoá phía client)" — so this writes RevokedAt to the DB;
        // a token that was never issued (or already revoked) is still a
        // 204, logout is idempotent.
        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    private async Task<TokenResponse> IssueTokensAsync(User user, IReadOnlyCollection<string> roles, CancellationToken ct)
    {
        var (accessToken, accessExpiresAt) = jwtTokenService.CreateAccessToken(user, roles);
        var (refreshPlain, refreshHash, refreshExpiresAt) = jwtTokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            TenantId = user.TenantId,
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpiresAt,
        });
        await db.SaveChangesAsync(ct);

        return new TokenResponse(accessToken, accessExpiresAt, refreshPlain, refreshExpiresAt);
    }
}
