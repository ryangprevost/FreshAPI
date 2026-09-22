using FreshApi.Application.Common.Interfaces;
using FreshApi.Domain.Common;
using FreshApi.Infrastructure.Persistence;

namespace FreshApi.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : Entity
    {
        if (_repositories.TryGetValue(typeof(T), out var repository))
        {
            return (IRepository<T>)repository;
        }

        var newRepository = new Repository<T>(_context);
        _repositories[typeof(T)] = newRepository;
        return newRepository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
