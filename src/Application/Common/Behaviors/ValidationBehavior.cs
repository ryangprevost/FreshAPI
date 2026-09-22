using System.Reflection;
using FreshApi.Domain.Common;
using FluentValidation;
using MediatR;

namespace FreshApi.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all FluentValidation validators for a request
/// before the handler executes. Validation failures short-circuit with a
/// <c>Validation.Failed</c> error instead of reaching the handler.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        var error = Error.Validation(
            "Validation.Failed",
            string.Join("; ", failures.Select(f => f.ErrorMessage)));

        return CreateFailure(error);
    }

    private static TResponse CreateFailure(Error error)
    {
        // TResponse is always Result or Result<T>; build the failure without
        // forcing every handler to repeat the mapping.
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = typeof(Result<>)
            .MakeGenericType(valueType)
            .GetMethod(nameof(Result<object>.Failure), BindingFlags.Public | BindingFlags.Static);

        return (TResponse)failureMethod!.Invoke(null, new object[] { error })!;
    }
}
