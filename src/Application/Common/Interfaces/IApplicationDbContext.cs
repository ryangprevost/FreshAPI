using FreshApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreshApi.Application.Common.Interfaces;

/// <summary>
/// Read-side access to the persistence model. Queries use this directly;
/// commands go through <see cref="IUnitOfWork"/>.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
