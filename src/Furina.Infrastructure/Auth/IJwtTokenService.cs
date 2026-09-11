using Furina.Domain.Entities;

namespace Furina.Infrastructure.Auth;

public interface IJwtTokenService
{
    /// <summary>Short-lived (15 min default) access token carrying tenant_id + role claims.</summary>
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(User user, IReadOnlyCollection<string> roles);

    /// <summary>
    /// Long-lived (7 day default) opaque refresh token. Returns the plain
    /// value (sent to the client once, never stored) and its SHA-256 hash
    /// (what actually goes in the DB, so a DB leak doesn't hand out usable
    /// tokens — TASK-12 DoD: "lưu HASH trong DB ... không lưu token thật").
    /// </summary>
    (string PlainToken, string Hash, DateTimeOffset ExpiresAt) CreateRefreshToken();

    string HashRefreshToken(string plainToken);
}
