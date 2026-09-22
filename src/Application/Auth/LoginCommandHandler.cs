using FreshApi.Domain.Common;
using MediatR;
using Microsoft.Extensions.Options;

namespace FreshApi.Application.Auth;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly DemoAuthOptions _demoAuth;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(IOptions<DemoAuthOptions> demoAuth, ITokenService tokenService)
    {
        _demoAuth = demoAuth.Value;
        _tokenService = tokenService;
    }

    public Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // TODO: DEMO CREDENTIALS — this validates against the "DemoAuth" config
        // section so the template works out of the box. Replace with a real user
        // store (ASP.NET Core Identity, your database, an external identity
        // provider) before going to production.
        if (!string.Equals(request.Username, _demoAuth.Username, StringComparison.Ordinal) ||
            !string.Equals(request.Password, _demoAuth.Password, StringComparison.Ordinal))
        {
            return Task.FromResult(Result<LoginResponse>.Failure(AuthErrors.InvalidCredentials));
        }

        var token = _tokenService.CreateToken(request.Username);
        return Task.FromResult(Result<LoginResponse>.Success(new LoginResponse(token.Token, token.ExpiresAtUtc)));
    }
}
