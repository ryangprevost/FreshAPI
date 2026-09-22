using FreshApi.Domain.Common;
using MediatR;

namespace FreshApi.Application.Auth;

/// <summary>
/// Authenticates a user and returns a bearer token.
/// </summary>
public sealed record LoginCommand(string Username, string Password) : IRequest<Result<LoginResponse>>;
