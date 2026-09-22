namespace FreshApi.Domain.Common;

/// <summary>
/// Marker for domain events raised by aggregates.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
