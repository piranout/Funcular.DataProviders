using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using EntityFramework.BulkInsert.Extensions;
using EntityFramework.Utilities;
using Funcular.DataProviders.EntityFramework.SqlServer;
using Funcular.ExtensionMethods;
using static System.Reflection.MethodBase;

namespace Funcular.DataProviders.EntityFramework
{
    public class ConcurrentEntityFrameworkProvider : EntityFrameworkProvider
    {
        protected readonly string _connectionString;

        protected static readonly ConcurrentDictionary<string, ThreadLocal<BaseContext>>
            _contextDictionary = new ConcurrentDictionary<string, ThreadLocal<BaseContext>>();

        public override BaseContext Context => _contextDictionary[this._connectionString].Value;

        public ConcurrentEntityFrameworkProvider(string connectionString) : base(connectionString)
        {
            var s = "MultipleActiveResultSets=true;";
            if (!(connectionString.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                connectionString = connectionString.EnsureEndsWith(";") + s;
            }
            _connectionString = connectionString;
            _contextDictionary.TryAdd(connectionString,
                new ThreadLocal<BaseContext>(() => GetContext(connectionString)));
        }

        private static BaseContext GetContext(string connectionString)
        {
            var baseContext = new BaseContext(connectionString);
            return baseContext;
        }

        /// <summary>
        ///     This constructor is not appropriate for concurrent scenarios. 
        /// </summary>
        /// <param name="context"></param>
        [Obsolete("This constructor is not appropriate for concurrent usage.", true)]
        public ConcurrentEntityFrameworkProvider(BaseContext context)
        {
            throw new NotImplementedException();
        }

        protected ConcurrentEntityFrameworkProvider()
        {
        }

        #region Overrides of EntityFrameworkProvider

        /// <summary>
        ///     Get all of the entities as an IQuerable which can then be
        ///     filtered even further.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="includes">
        ///     Descendant objects to include. Note that descendants do not need
        ///     to be included or fetched in order to reference them in a query predicate.
        /// </param>
        /// <returns></returns>
        public override IQueryable<TEntity> Query<TEntity>(params Expression<Func<TEntity, object>>[] includes)
        {
            var methodName = GetCurrentMethod().Name;
            var threadId = Thread.CurrentThread.ManagedThreadId;
            Debug.WriteLine($"Thread {threadId} entered {methodName}.");
            var context = new BaseContext(_nameOrConnectionString);

            IQueryable<TEntity> returnValue = context.Set<TEntity>();// GetDbSet<TEntity>();
            if (includes == null)
                return returnValue;
            foreach (var include in includes)
            {
                returnValue = returnValue.Include(include);
            }
            Debug.WriteLine($"Thread {threadId} leaving {methodName}.");
            return returnValue;

        }

        #region Overrides of EntityFrameworkProvider

        /// <summary>
        ///     Add a collection of entity instances to the data context
        ///     and commits the transaction.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entities"></param>
        /// <returns></returns>
        public override IEnumerable<TEntity> BulkInsert<TEntity, TId>(ICollection<TEntity> entities)
        {
            using (var context = new BaseContext(_nameOrConnectionString))
            {
                var createables = SetCreatableProperties<TEntity, TId>(entities).ToArray();
                // var dbSet = GetContext(_connectionString).Set<TEntity>();// GetDbSet<TEntity>();
                // EFBatchOperation.For(Context, dbSet).InsertAll(createables);
                context.BulkInsert(createables);
                return createables;
            }

        }

        #endregion

        #endregion
    }
}