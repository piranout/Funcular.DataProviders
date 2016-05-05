using System;
using System.Linq;
using System.Linq.Expressions;

namespace Funcular.DataProviders.EntityFramework
{
    public interface IConcurrentEntityProvider : IEntityProvider
    {
        IQueryable<TEntity> Query<TEntity>(IContextDisposer disposer, params Expression<Func<TEntity, object>>[] includes)
            where TEntity : class, new();

        Action<string> Log { get; set; }
    }
}