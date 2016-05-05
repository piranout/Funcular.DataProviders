using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using EntityFramework.BulkInsert.Extensions;
using EntityFramework.Utilities;
using Funcular.DataProviders.EntityFramework.SqlServer;
using Funcular.ExtensionMethods;
using Funcular.Ontology.Archetypes;
using static System.Reflection.MethodBase;

namespace Funcular.DataProviders.EntityFramework
{
    public class ConcurrentEntityFrameworkProvider : EntityFrameworkProvider, IConcurrentEntityProvider
    {
        protected readonly string _connectionString;

        protected static readonly ConcurrentDictionary<string, ThreadLocal<BaseContext>>
            _contextDictionary = new ConcurrentDictionary<string, ThreadLocal<BaseContext>>();

        protected static Action<string> _log;

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
                new ThreadLocal<BaseContext>(() => GetNewContext(connectionString)));
        }

        private static BaseContext GetNewContext(string connectionString)
        {
            var baseContext = new BaseContext(connectionString);
            baseContext.Database.Log = _log;
            return baseContext;
        }

        /// <summary>
        /// Gets a new context with the connection string that is already 
        /// associated with this instance.
        /// </summary>
        /// <returns></returns>
        public BaseContext GetContext()
        {
            return GetNewContext(_connectionString);
        }

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member

        /// <summary>
        ///     This constructor is not appropriate for concurrent scenarios. 
        /// </summary>
        /// <param name="context"></param>
        [Obsolete("This constructor is not appropriate for concurrent usage scenarios.", true)]
        public ConcurrentEntityFrameworkProvider(BaseContext context)
        {
            throw new NotImplementedException();
        }

        protected ConcurrentEntityFrameworkProvider()
        {
        }

        public Action<string> Log { get {return _log;} set { _log = value; } }

        /// <summary>
        ///     Get an entity by its Id
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public override TEntity Get<TEntity, TKey>(TKey id)
        {
            using (var context = GetContext())
            {
                return context.Set<TEntity>().Find(id);
            }
        }

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
            var context = GetContext();
            IQueryable<TEntity> returnValue = context.Set<TEntity>();// GetDbSet<TEntity>();
            if (includes == null)
                return returnValue;
            returnValue = includes.Aggregate(returnValue, (current, include) => current.Include<TEntity, object>(include));
            Debug.WriteLine($"Thread {threadId} leaving {methodName}.");
            return returnValue;

        }

        public IQueryable<TEntity> Query<TEntity>(IContextDisposer disposer, params Expression<Func<TEntity, object>>[] includes) where TEntity : class, new()
        {
            var methodName = GetCurrentMethod().Name;
            var threadId = Thread.CurrentThread.ManagedThreadId;
            Debug.WriteLine($"Thread {threadId} entered {methodName}.");
            var context = GetContext();
            disposer.Contexts.Add(context);
            IQueryable<TEntity> returnValue = context.Set<TEntity>();// GetDbSet<TEntity>();
            if (includes == null)
                return returnValue;
            returnValue = includes.Aggregate(returnValue, (current, include) => current.Include<TEntity, object>(include));
            Debug.WriteLine($"Thread {threadId} leaving {methodName}.");
            return returnValue;
        }

        /// <summary>
        ///     Throws an InvalidOperationException in the concurrent provider.
        /// Works in the single-threaded base class though.
        /// </summary>
        /// <param name="entity"></param>
        [Obsolete("Add is not an appropriate operation for a concurrent provider" +
                  " because it disposes its context on each call.",true)]
        public override void Add<TEntity, TId>(TEntity entity)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Add a new entity instance to the database and commit.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entities"></param>
        /// <param name="safe">
        ///     Return after entity is saved. If false, attempts to save async, in which case
        ///     it is possible to encounter silent exceptions.
        /// </param>
        /// <returns></returns>
        public override IEnumerable<TEntity> Insert<TEntity, TId>(ICollection<TEntity> entities, bool safe = true)
        {
            using(var context = GetNewContext(_connectionString))
            {
                foreach (var entity in entities)
                {
                    context.Set<TEntity>().Add(entity);
                }
                if (safe)
                    context.SaveChanges();
                else
                    context.SaveChangesAsync();
            }
            return entities;
        }

        /// <summary>
        ///     Add a new entity instance to the database and commit.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entity"></param>
        /// <param name="safe">
        ///     Return after entity is saved. If false, attempts to save async, in which case
        ///     it is possible to encounter silent exceptions.
        /// </param>
        /// <returns></returns>
        public override TEntity Insert<TEntity, TId>(TEntity entity, bool safe = true)
        {
            var createable = entity as ICreateable<TId>;
            if(createable != null)
                SetCreateableProperties<TId>(createable);
            using (var context = GetContext())
            {
                context.Configuration.AutoDetectChangesEnabled = true;
                context.Set<TEntity>().Add(entity);
                SetCreatableModifyableProperties<TId>(context);
                //context.Entry(entity).State = EntityState.Added;////    context.Set<TEntity>().Add(entity);
                if (safe)
                    context.SaveChanges();
                else
                    context.SaveChangesAsync();
            }
            return entity;
        }

        /// <summary>
        ///     Update a collection of entity instances in the database, and commit once.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entities"></param>
        /// <param name="safe">
        ///     Return after entity is saved. If false, attempts to save async, in which case
        ///     it is possible to encounter silent exceptions.
        /// </param>
        /// <returns></returns>
        public override IEnumerable<TEntity> Update<TEntity, TId>(ICollection<TEntity> entities, bool safe = true)
        {
            var entityArray = SetModifyableProperties<TEntity, TId>(entities).ToArray();
            using (var context = GetContext())
            {
                foreach (var entity in entityArray)
                {
                    context.AttachAndModify(entity);
                    var dbEntityEntry = this.Context.Entry(entity);
                    dbEntityEntry.State = EntityState.Modified;
                }
                if (safe)
                    context.SaveChanges();
                else
                    context.SaveChangesAsync();
            }
            return entityArray;
        }

        /// <summary>
        ///     Update an entity instance in the database and commit.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entity"></param>
        /// <param name="safe">
        ///     Return after entity is saved. If false, attempts to save async, in which case
        ///     it is possible to encounter silent exceptions.
        /// </param>
        /// <returns></returns>
        public override TEntity Update<TEntity, TId>(TEntity entity, bool safe = true)
        {
            SetModifyableProperties<TId>(entity as IModifyable<TId>);
            using (var context = GetContext())
            {
                
                context.Entry(entity).State = EntityState.Modified;
                /*var dbEntityEntry = this.Context.Entry(entity);
                    dbEntityEntry.State = EntityState.Modified;*/
                
                if (safe)
                    context.SaveChanges();
                else
                    context.SaveChangesAsync();
            }
            return entity;
        }

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
            using (var context = GetContext())
            {
                context.Set<TEntity>().AddRange(entities);
                SetCreatableModifyableProperties<TId>(context);
                // SetCreatableProperties<TEntity, TId>(entities).ToArray();
            }
            using (var context = GetContext())
            {
                context.BulkInsert(entities);
            }
                
            return entities;
            

        }

        public override int BulkDelete<TEntity>(Expression<Func<TEntity, bool>> predicate)
        {
            using(var context = GetContext())
            {
                return EFBatchOperation.For(context, context.Set<TEntity>())
                    .Where(predicate)
                    .Delete();
            }

        }

        public override void BulkUpdate<TEntity, TProp>(Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TProp>> propertyExpression, Expression<Func<TEntity, TProp>> assignmentExpression)
        {
            using (var context = GetContext())
            {
                EFBatchOperation.For(context, context.Set<TEntity>())
                    .Where(predicate)
                    .Update(propertyExpression, assignmentExpression);
            }
        }

        /// <summary>
        ///     Delete an entity.
        /// </summary>
        /// <param name="entity"></param>
        public override void Delete<TEntity, TId>(TEntity entity)
        {
            using (var context = GetContext())
            {
                var dbSet = context.Set<TEntity>();
                dbSet.Attach(entity);
                dbSet.Remove(entity);
                context.SaveChanges();
            }
            
        }

        /// <summary>
        ///     Delete an entity by id.
        /// </summary>
        /// <param name="id"></param>
        public override void Delete<TEntity, TId>(TId id)
        {
            using (var context = GetContext())
            {
                var entry = context.Set<TEntity>().Find(id);
                if (entry == null) return;
                context.Set<TEntity>().Remove(entry);
                context.SaveChanges();
            }
        }

        /// <summary>
        ///     Saves all modifications to the context without enforcing business logic.
        ///     Use sparingly and in special cases only; prefer the typed options.
        /// </summary>
        [Obsolete("This operation requires a single context instance, " +
                  "which is not available in the concurrent provider", true)]
        public override void Save()
        {
            throw new InvalidOperationException("");
        }

        /// <summary>
        /// Throws an invalid operation exception.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        [Obsolete("This operation requires a single context instance, " +
                  "which is not available in the concurrent provider", true)]
        public override int SaveChanges<TId>()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Throws an invalid operation exception.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        [Obsolete("This operation requires a single context instance, " +
                  "which is not available in the concurrent provider", true)]
        public override void SaveChangesAsync<TId>()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Throws an InvalidOperationException.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        [Obsolete("This operation requires a single context instance, " +
                  "which is not available in the concurrent provider", true)]
        public override void SetModified<TEntity>(TEntity entity)
        {
            base.SetModifyableProperties(entity as IModifyable<TEntity>);
        }

        /// <summary>
        /// You must use GetContext.Set<typeparam name="TEntity"></typeparam> for concurrent providers,
        ///  and manage disposal of the context in the calling method.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        [Obsolete("You must use GetContext.Set<TEntity> for concurrent providers" +
                  " and manage disposal of the context in the calling method.", true)]
        public override DbSet<TEntity> GetDbSet<TEntity>() 
        {
            throw new InvalidOperationException("");
        }

        /// <summary>
        ///     Get a reference to the context’s underlying database.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Not valid in a concurrent scenario with multiple contexts", error: true)]
        public override Database GetDatabase()
        {
            throw new NotImplementedException();
        }


        protected void SetCreatableModifyableProperties<TId>(BaseContext context)
        {
            Func<DbEntityEntry, bool> predicate = entry => entry.State == EntityState.Added || entry.State == EntityState.Modified;
            if (!context.ChangeTracker.Entries().Any(predicate))
                return;
            foreach (var dbEntityEntry in context.ChangeTracker.Entries<ICreateable<TId>>().Where(entry => predicate(entry)))
            {
                switch (dbEntityEntry.State)
                {
                    case EntityState.Added:
                        SetCreateableProperties<TId>(dbEntityEntry.Entity);
                        break;
                    case EntityState.Modified:
                        var modifyable = dbEntityEntry.Entity as IModifyable<TId>;
                        if (modifyable != null)
                        {
                            SetModifyableProperties<TId>(modifyable);
                        }
                        break;
                }
            }
        }
    }
}