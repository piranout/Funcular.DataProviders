#region File info
// *********************************************************************************************************
// Funcular.DataProviders>Funcular.DataProviders>EntityFrameworkProvider.cs
// Created: 2015-07-01 3:52 PM
// Updated: 2015-07-06 10:22 AM
// By: Paul Smith 
// 
// *********************************************************************************************************
// LICENSE: The MIT License (MIT)
// *********************************************************************************************************
// Copyright (c) 2010-2015 <copyright holders>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// *********************************************************************************************************
#endregion


#region Usings
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using EntityFramework.BulkInsert.Extensions;
using EntityFramework.Utilities;
using Funcular.DataProviders.EntityFramework.SqlServer;
using Funcular.Ontology.Archetypes;
using static System.Reflection.MethodBase;

#endregion


namespace Funcular.DataProviders.EntityFramework
{
    public class EntityFrameworkProvider : IEntityProvider, IDisposable
    {
        #region Instance members
        protected readonly ConcurrentDictionary<Type, object> _dbSets =
            new ConcurrentDictionary<Type, object>();
        protected bool _disposed;

        protected BaseContext _context;

        protected object _currentUser;
        protected string _nameOrConnectionString;

        // TODO:
        // Static ConcurrentDictionary<string,ThreadLocal<BaseContext>>; (key = connection string)
        public virtual BaseContext Context { get { return this._context; } }

        /// <summary>
        ///     Get a reference to the context’s underlying database.
        /// </summary>
        /// <returns></returns>
        public virtual Database GetDatabase()
        {
            return this.Context.Database;
        }
        #endregion


        #region Constructors
        public EntityFrameworkProvider(string connectionString)
        {
            _nameOrConnectionString = connectionString;
            var context = new BaseContext(connectionString);
            this._context = context;
#if DEBUG
            Configuration.Log = s => Debug.WriteLine(s);
            _context.Database.Log = s => Debug.WriteLine(s);
#endif

        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="context"></param>
        public EntityFrameworkProvider(BaseContext context)
        {
            this._context = context;
#if DEBUG
            Configuration.Log = s => Debug.WriteLine(s);
            _context.Database.Log = s => Debug.WriteLine(s);
#endif
        }

        protected EntityFrameworkProvider()
        {
        }

        #endregion


        #region IEntityProvider implementation
        public virtual Func<Type, bool> IsEntityType { get { return Context.IsEntityType; } set { this.Context.IsEntityType = value; } }

        /// <summary>
        ///     Set the user Id to use when updating ICreateable and IModifyable entities
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <param name="user"></param>
        public virtual void SetCurrentUser<TId>(TId user)
        {
            this._currentUser = user;
        }

        /// <summary>
        ///     Get the current user assigned to this context
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        public virtual TId GetCurrentUser<TId>()
        {
            return (TId)(this._currentUser ?? (object)"Unknown");
        }

        /// <summary>
        ///     Get an entity by its Id
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public TEntity Get<TEntity, TKey>(TKey id) where TEntity : class, new()
        {
            return GetDbSet<TEntity>().Find(id);
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
        public virtual IQueryable<TEntity> Query<TEntity>(params Expression<Func<TEntity, object>>[] includes)
            where TEntity : class, new()
        {
            var methodName = GetCurrentMethod().Name;
            var threadId = Thread.CurrentThread.ManagedThreadId;
            Debug.WriteLine($"Thread {threadId} entered {methodName}.");
            

                IQueryable<TEntity> returnValue = GetDbSet<TEntity>();
                if (includes == null)
                    return returnValue;
                foreach (var include in includes)
                {
                    returnValue = returnValue.Include(include);
                }
                Debug.WriteLine($"Thread {threadId} leaving {methodName}.");
                return returnValue;
                
            
        }

        /// <summary>
        ///     Adds an entity to the context without saving.
        /// </summary>
        /// <param name="entity"></param>
        public virtual void Add<TEntity, TId>(TEntity entity) where TEntity : class, new()
        {
            if (Context.Entry(entity).State != EntityState.Detached)
                return;
            GetDbSet<TEntity>().Add(entity);
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
        public virtual TEntity Insert<TEntity, TId>(TEntity entity, bool safe = true) where TEntity : class, new()
        {
            GetDbSet<TEntity>().Add(entity);
            if (safe)
                SaveChanges<TId>();
            else
                SaveChangesAsync<TId>();
            return entity;
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
        public virtual IEnumerable<TEntity> Insert<TEntity, TId>(ICollection<TEntity> entities, bool safe = true) where TEntity : class, new()
        {
            var entityArray = entities.ToArray();
            foreach (var entity in entityArray)
            {
                GetDbSet<TEntity>().Add(entity);
            }
            if (safe)
                SaveChanges<TId>();
            else
                SaveChangesAsync<TId>();
            return entityArray;
        }

        /// <summary>
        ///     Add a collection of entity instances to the data context
        ///     and commits the transaction.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entities"></param>
        /// <returns></returns>
        public virtual IEnumerable<TEntity> BulkInsert<TEntity, TId>(ICollection<TEntity> entities) where TEntity : class, new()
        {
            var createables = SetCreatableProperties<TEntity, TId>(entities).ToArray();
            // var dbSet = GetDbSet<TEntity>();
            Context.BulkInsert(createables);
            // EFBatchOperation.For(Context, dbSet).InsertAll(createables);
            return createables;
        }

        public virtual int BulkDelete<TEntity>(Expression<Func<TEntity,bool>> predicate) where TEntity : class, new()
        {
            return EFBatchOperation.For(Context, GetDbSet<TEntity>())
                .Where(predicate)
                .Delete();
        }

        /*
                /// <summary>
                /// Updates a single property of the entity without committing.  You must 
                /// call <see cref="Save"/> afterwards to execute the update command.
                /// </summary>
                /// <typeparam name="TEntity"></typeparam>
                /// <typeparam name="TId"></typeparam>
                /// <typeparam name="TProperty"></typeparam>
                /// <param name="entity"></param>
                /// <param name="expression"></param>
                /// <param name="value"></param>
                public void BulkUpdate<TEntity, TId, TProperty>(TEntity entity, Expression<Func<TEntity, TProperty>> expression,
                    TProperty value) where TEntity : class, IIdentity<TId>, new()
                {
                    var id = new TEntity() {Id = entity.Id};
                    SetModifyableProperties<TId>(entity as IModifyable<TId>);
                    Context.AttachAndModify<TEntity>(id).Set<TProperty>(expression, value);
                }


                /// <summary>
                /// Updates multiple properties of the entity without committing. You must 
                /// call <see cref="Save"/> afterwards to execute the update command.
                /// </summary>
                /// <typeparam name="TEntity"></typeparam>
                /// <typeparam name="TId"></typeparam>
                /// <typeparam name="TProperty"></typeparam>
                /// <param name="entity"></param>
                /// <param name="expressions"></param>
                /// <param name="value"></param>
                public void BulkUpdate<TEntity, TId, TProperty>(TEntity entity, ICollection<Expression<Func<TEntity, TProperty>>> expressions,
                    TProperty value) where TEntity : class, IIdentity<TId>, new()
                {
                    var id = new TEntity() { Id = entity.Id };
                    SetModifyableProperties(entity as IModifyable<TId>);
                    var attachAndModifyContext = Context.AttachAndModify<TEntity>(id);
                    foreach (var expression in expressions)
                    {
                        attachAndModifyContext.Set<TProperty>(expression, value);
                    }
                }*/

        public virtual void BulkUpdate<TEntity,TProp>(
            Expression<Func<TEntity,bool>> predicate,
            Expression<Func<TEntity,TProp>> propertyExpression, 
            Expression<Func<TEntity, TProp>> assignmentExpression) where TEntity : class, new()
        {
            EFBatchOperation.For(Context, GetDbSet<TEntity>())
                .Where(predicate)
                .Update(propertyExpression, assignmentExpression);
            
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
        public virtual TEntity Update<TEntity, TId>(TEntity entity, bool safe = true) where TEntity : class, new()
        {
            var dbEntityEntry = this.Context.Entry(entity);
            if (dbEntityEntry.State == EntityState.Detached)
                GetDbSet<TEntity>().Attach(entity);
            dbEntityEntry.State = EntityState.Modified;
            if (safe)
                SaveChanges<TId>();
            else
                SaveChangesAsync<TId>();
            return dbEntityEntry.Entity;
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
        public virtual IEnumerable<TEntity> Update<TEntity, TId>(ICollection<TEntity> entities, bool safe = true) where TEntity : class, new()
        {
            var entityArray = SetModifyableProperties<TEntity, TId>(entities).ToArray();
            foreach (var entity in entityArray)
            {
                var dbEntityEntry = this.Context.Entry(entity);
                if (dbEntityEntry.State == EntityState.Detached)
                    GetDbSet<TEntity>().Attach(entity);
                dbEntityEntry.State = EntityState.Modified;
            }
            if (safe)
                SaveChanges<TId>();
            else
                SaveChangesAsync<TId>();
            return entityArray;
        }

        /// <summary>
        ///     Delete an entity.
        /// </summary>
        /// <param name="entity"></param>
        public virtual void Delete<TEntity, TId>(TEntity entity) where TEntity : class, new()
        {
            var dbSet = GetDbSet<TEntity>();
            dbSet.Attach(entity);
            dbSet.Remove(entity);
            SaveChanges<TId>();
        }

        /// <summary>
        ///     Delete an entity by id.
        /// </summary>
        /// <param name="id"></param>
        public void Delete<TEntity, TId>(TId id) where TEntity : class, new()
        {
            var entity = Get<TEntity, TId>(id);
            GetDbSet<TEntity>().Remove(entity);
            SaveChanges<TId>();
        }

        /// <summary>
        ///     Saves all modifications to the context without enforcing business logic.
        ///     Use sparingly and in special cases only; prefer the typed options.
        /// </summary>
        public virtual void Save()
        {
            this.Context.SaveChanges();
        }

        /// <summary>
        ///     Set the context’s entry for this instance to EntityState.Modified.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        public void SetDetached<TEntity>(TEntity entity) where TEntity : class
        {
            this.Context.Entry(entity).State = EntityState.Detached;
        }

        /// <summary>
        ///     Set the context’s entry for this instance to EntityState.Modified.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        public virtual void SetModified<TEntity>(TEntity entity) where TEntity : class
        {
            this.Context.Entry(entity).State = EntityState.Modified;
        }

        /// <summary>
        ///     Set the context’s entry for this instance to EntityState.Modified.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        public void SetAdded<TEntity>(TEntity entity) where TEntity : class
        {
            this.Context.Entry(entity).State = EntityState.Added;
        }

        /// <summary>
        /// Runs business logic on Ontology-derived entities and saves changes.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        public virtual int SaveChanges<TId>()
        {
            SetCreatableModifyableProperties<TId>();
            try
            {
                return this.Context.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException e)
            {
                Console.WriteLine(e);
                return -1;
            }
        }

        /// <summary>
        /// Runs business logic on Ontology-derived entities and saves changes.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        public virtual void SaveChangesAsync<TId>()
        {
            SetCreatableModifyableProperties<TId>();
            Context.SaveChangesAsync().Start();
        }

        /// <summary>
        ///     Get the DbSet for a specific entity type
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public DbSet<TEntity> GetDbSet<TEntity>() where TEntity : class, new()
        {
            return (DbSet<TEntity>)this._dbSets.GetOrAdd(typeof(TEntity), x => Context.Set<TEntity>());
        }

        #endregion

        protected virtual void SetCreatableModifyableProperties<TId>()
        {
            var context = this.Context;
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

        protected virtual void SetCreateableProperties<TId>(ICreateable<TId> createable)
        {
            if (createable.CreatedBy == null || createable.CreatedBy.Equals(default(TId)) == true)
                createable.CreatedBy = GetCurrentUser<TId>();
            if (createable.DateCreatedUtc == default(DateTime))
                createable.DateCreatedUtc = DateTime.UtcNow;
        }

        protected virtual IEnumerable<TEntity> SetCreatableProperties<TEntity, TId>(ICollection<TEntity> entities) where TEntity : class, new()
        {
            var createables = entities
                .OfType<ICreateable<TId>>()
                .Select(item => item)
                .ToArray();
            foreach (var createable in createables)
            {
                SetCreateableProperties(createable);
            }
            return entities;
        }

        protected virtual IEnumerable<TEntity> SetModifyableProperties<TEntity, TId>(ICollection<TEntity> entities) where TEntity : class, new()
        {
            var modifyables = entities
                .OfType<IModifyable<TId>>()
                .ToArray();
            foreach (var modifyable in modifyables)
            {
                SetModifyableProperties(modifyable);
            }
            return entities;
        }

        protected virtual void SetModifyableProperties<TId>(IModifyable<TId> modifyable)
        {
            if (modifyable == null)
                return;
            modifyable.DateModifiedUtc = DateTime.UtcNow;
            modifyable.ModifiedBy = GetCurrentUser<TId>();
        }

        #region IDisposable implementation
        private void dispose(bool disposing)
        {
            if (this._disposed)
                return;
            if (disposing)
            {
                try
                {
                    if (this.Context == null)
                        return;
                    this.Context.Dispose();
                    this._context = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            this._disposed = true;
        }

        /// <summary>
        ///     Dispose of this class and cleanup the DbContext
        /// </summary>
        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}