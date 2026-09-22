using System.Linq.Expressions;
using FreshApi.Domain.Common;

namespace FreshApi.Application.Common.Interfaces;

/// <summary>
/// Generic repository abstraction. Keep it small on purpose:
/// complex reads belong on <see cref="IApplicationDbContext"/> (or a dedicated read model),
/// not behind an ever-growing generic interface.
/// </summary>
public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);

    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    void Update(T entity);

    void Remove(T entity);
}
