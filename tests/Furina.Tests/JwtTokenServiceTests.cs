using System.IdentityModel.Tokens.Jwt;
using Furina.Domain.Entities;
using Furina.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Xunit;

namespace Furina.Tests;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(Options.Create(new JwtOptions
    {
        Secret = "unit-test-secret-at-least-32-bytes-long!!",
        Issuer = "furina-test",
        Audience = "furina-test-clients",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7,
    }));

    private static User SampleUser() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Email = "owner@demo-clinic.furina.local",
    };

    [Fact]
    public void CreateAccessToken_EmbedsTenantIdAndRoleClaims()
    {
        var service = CreateService();
        var user = SampleUser();

        var (token, expiresAt) = service.CreateAccessToken(user, [RoleNames.Owner]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(user.TenantId.ToString(), jwt.Claims.Single(c => c.Type == JwtTokenService.TenantIdClaimType).Value);
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == RoleNames.Owner);
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.True(expiresAt > DateTimeOffset.UtcNow.AddMinutes(14));
        Assert.True(expiresAt <= DateTimeOffset.UtcNow.AddMinutes(15));
    }

    [Fact]
    public void CreateRefreshToken_HashMatchesHashRefreshTokenOfThePlainValue()
    {
        var service = CreateService();

        var (plainToken, hash, expiresAt) = service.CreateRefreshToken();

        Assert.Equal(hash, service.HashRefreshToken(plainToken));
        Assert.True(expiresAt > DateTimeOffset.UtcNow.AddDays(6));
    }

    [Fact]
    public void CreateRefreshToken_NeverReturnsThePlainTokenAsTheStoredHash()
    {
        // TASK-12 DoD: "lưu HASH trong DB ... không lưu token thật" — this
        // is the one invariant that must never regress.
        var service = CreateService();

        var (plainToken, hash, _) = service.CreateRefreshToken();

        Assert.NotEqual(plainToken, hash);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic()
    {
        var service = CreateService();

        var hash1 = service.HashRefreshToken("some-plain-refresh-token");
        var hash2 = service.HashRefreshToken("some-plain-refresh-token");

        Assert.Equal(hash1, hash2);
    }
}
