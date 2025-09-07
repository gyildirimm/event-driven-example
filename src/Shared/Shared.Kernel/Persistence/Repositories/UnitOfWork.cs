using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.Application.Repositories;
using Shared.Kernel.Domain;

namespace Shared.Kernel.Persistence.Repositories;

public class UnitOfWork<TContext> : IUnitOfWork<TContext>
    where TContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, IDbContextTransaction> _transactions;
    private bool _disposed;

    public UnitOfWork(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _transactions = new ConcurrentDictionary<Type, IDbContextTransaction>();
    }

    public DbContext GetDbContext()
    {
        var context = _serviceProvider.GetRequiredService<TContext>();
        return context;
    }

    public IRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        var context = _serviceProvider.GetRequiredService<TContext>();
        
        // Her çağrıda yeni repository instance oluştur - lightweight ve memory safe
        return new EfRepositoryBase<TEntity, TKey, TContext>(context);
    }

    public IAsyncRepository<TEntity, TKey> GetAsyncRepository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        var context = _serviceProvider.GetRequiredService<TContext>();
        
        // Her çağrıda yeni repository instance oluştur - lightweight ve memory safe
        return new EfRepositoryBase<TEntity, TKey, TContext>(context);
    }

    public int SaveChanges()
    {
        var context = _serviceProvider.GetRequiredService<TContext>();
        return context.SaveChanges();
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var context = _serviceProvider.GetRequiredService<TContext>();
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var context = _serviceProvider.GetRequiredService<TContext>() as DbContext;
        if (context == null)
        {
            throw new InvalidOperationException($"Context {typeof(TContext).Name} is not a DbContext");
        }

        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        _transactions.TryAdd(typeof(TContext), transaction);
        return transaction;
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
            await transaction.DisposeAsync();
        }
    }

    public async Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction != null)
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var transaction in _transactions.Values)
            {
                transaction?.Dispose();
            }
            _transactions.Clear();

            _disposed = true;
        }
    }
}
