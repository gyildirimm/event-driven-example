using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Shared.Kernel.Application.OperationResults.Paging;
using Shared.Kernel.Application.Repositories.DynamicAction;
using Shared.Kernel.Domain;

namespace Shared.Kernel.Application.Repositories;

public interface IAsyncRepository<T, TKey> : IQuery<T> 
    where T :  class, IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate);
    
    // Geliştirilmiş GetAsync metodu
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        bool enableTracking = true,
        CancellationToken cancellationToken = default);

    Task<IPaginate<T>> GetListAsync(Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        int index = 0, int size = 10, bool enableTracking = true,
        CancellationToken cancellationToken = default);

    Task<IPaginate<T>> GetListByDynamicAsync(Dynamic dynamic,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        int index = 0, int size = 10, bool enableTracking = true,
        CancellationToken cancellationToken = default);

    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<T> DeleteAsync(T entity);
    
    Task<IList<T>> AddRangeAsync(IList<T> entities, CancellationToken cancellationToken = default);
    Task<IList<T>> UpdateRangeAsync(IList<T> entities, CancellationToken cancellationToken = default);
    Task<IList<T>> DeleteRangeAsync(IList<T> entities, CancellationToken cancellationToken = default);
}