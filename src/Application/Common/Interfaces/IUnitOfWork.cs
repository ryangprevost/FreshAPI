using FreshApi.Domain.Common;

namespace FreshApi.Application.Common.Interfaces;

/// <summary>
/// Unit of work: one repository per aggregate, one atomic save.
/// </summary>
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : Entity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
