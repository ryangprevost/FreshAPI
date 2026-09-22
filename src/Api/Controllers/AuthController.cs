using FreshApi.Api.Common;
using FreshApi.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FreshApi.Api.Controllers;

/// <summary>
/// Issues JWT bearer tokens. Demo credentials come from the "DemoAuth"
/// configuration section — replace with a real user store before production.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }
}
