namespace FreshApi.Domain.Exceptions;

/// <summary>
/// Thrown when an invariant is violated in a place where the Result pattern
/// cannot be used (e.g. inside infrastructure code). Handled globally by the
/// API exception-handling middleware and mapped to HTTP 422.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
