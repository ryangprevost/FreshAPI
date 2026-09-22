namespace FreshApi.Application.Auth;

/// <summary>
/// Bearer token issued by POST /api/auth/login.
/// </summary>
public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc);
