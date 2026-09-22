namespace FreshApi.Application.Auth;

/// <summary>
/// Binds the "DemoAuth" configuration section.
/// <para>
/// TODO: DEMO CREDENTIALS — replace with a real user store (ASP.NET Core
/// Identity, your database, or an external identity provider) before going to
/// production. Never ship real user passwords in configuration.
/// </para>
/// </summary>
public sealed class DemoAuthOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
