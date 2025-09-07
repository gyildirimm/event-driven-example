using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Kernel.Domain;

namespace Shared.Kernel.Application.Repositories;

public interface IUnitOfWork : IDisposable
{
    DbContext GetDbContext();
    
    /// <summary>
    /// Get repository for specific entity
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Entity key type</typeparam>
    /// <returns>Repository instance</returns>
    IRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>;

    /// <summary>
    /// Get async repository for specific entity
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Entity key type</typeparam>
    /// <returns>Async repository instance</returns>
    IAsyncRepository<TEntity, TKey> GetAsyncRepository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>
        where TKey : IEquatable<TKey>;

    /// <summary>
    /// Save changes for the context
    /// </summary>
    /// <returns>Number of affected rows</returns>
    int SaveChanges();

    /// <summary>
    /// Save changes for the context asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begin transaction for the context
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transaction scope</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit transaction
    /// </summary>
    /// <param name="transaction">Transaction to commit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CommitTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback transaction
    /// </summary>
    /// <param name="transaction">Transaction to rollback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RollbackTransactionAsync(IDbContextTransaction transaction, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork<TContext> : IUnitOfWork
    where TContext : DbContext
{
}