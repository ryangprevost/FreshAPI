using FreshApi.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace FreshApi.Api.Common;

/// <summary>
/// Maps Result/Result&lt;T&gt; to HTTP responses:
/// success -> 200/204, *.NotFound -> 404, Auth.InvalidCredentials -> 401,
/// everything else -> 400.
/// </summary>
public static class ResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return MapError(result.Error);
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return MapError(result.Error);
    }

    private static ActionResult MapError(Error error)
    {
        var body = new { error.Code, error.Message };

        return error.Code switch
        {
            "Product.NotFound" => new NotFoundObjectResult(body),
            "Auth.InvalidCredentials" => new UnauthorizedObjectResult(body),
            _ => new BadRequestObjectResult(body),
        };
    }
}
