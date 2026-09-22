using FreshApi.Domain.Common;

namespace FreshApi.Domain.Events;

/// <summary>
/// Raised when a new product is created.
/// </summary>
public sealed record ProductCreatedDomainEvent(Guid ProductId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
