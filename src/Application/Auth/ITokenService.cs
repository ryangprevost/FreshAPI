namespace FreshApi.Application.Auth;

/// <summary>
/// Mints JWT bearer tokens for authenticated users.
/// </summary>
public interface ITokenService
{
    TokenResult CreateToken(string username);
}

/// <summary>
/// A minted token and when it expires (UTC).
/// </summary>
public sealed record TokenResult(string Token, DateTime ExpiresAtUtc);
