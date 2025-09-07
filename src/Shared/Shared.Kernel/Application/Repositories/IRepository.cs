using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Shared.Kernel.Application.OperationResults.Paging;
using Shared.Kernel.Application.Repositories.DynamicAction;
using Shared.Kernel.Domain;

namespace Shared.Kernel.Application.Repositories;

public interface IRepository<T, TKey> : IQuery<T> 
    where T : class, IEntity<TKey> 
    where TKey : IEquatable<TKey>
{
    T? Get(Expression<Func<T, bool>> predicate);
    
    // Geliştirilmiş Get metodu
    T? Get(Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        bool enableTracking = true);

    IPaginate<T> GetList(Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        int index = 0, int size = 10,
        bool enableTracking = true);

    IPaginate<T> GetListByDynamic(Dynamic dynamic,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        int index = 0, int size = 10, bool enableTracking = true);

    T Add(T entity);
    T Update(T entity);
    T Delete(T entity);
    
    // Toplu işlemler için yeni metodlar
    IList<T> AddRange(IList<T> entities);
    IList<T> UpdateRange(IList<T> entities);
    IList<T> DeleteRange(IList<T> entities);
}