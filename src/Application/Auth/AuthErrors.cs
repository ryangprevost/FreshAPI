using FreshApi.Domain.Common;

namespace FreshApi.Application.Auth;

public static class AuthErrors
{
    public static Error InvalidCredentials =>
        Error.Validation("Auth.InvalidCredentials", "The username or password is incorrect.");
}
