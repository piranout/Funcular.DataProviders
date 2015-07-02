#region File info
// *********************************************************************************************************
// Funcular.DataProviders>Funcular.DataProviders>EntityFrameworkProvider.cs
// Created: 2015-07-01 3:52 PM
// Updated: 2015-07-02 2:40 PM
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
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Funcular.DataProviders.EntityFramework.SqlServer;
using Funcular.Ontology.Archetypes;
#endregion


namespace Funcular.DataProviders.EntityFramework
{
    public class EntityFrameworkProvider : IEntityProvider, IDisposable
    {
        #region Instance members
        private readonly ConcurrentDictionary<Type, object> _dbSets =
            new ConcurrentDictionary<Type, object>();

        private BaseContext _context;

        protected object _currentUser;
        private bool _disposed;

        public BaseContext Context { get { return this._context; } }

        /// <summary>
        /// Get a reference to the context’s underlying database.
        /// </summary>
        /// <returns></returns>
        public Database GetDatabase()
        {
            return this._context.Database;
        }
        #endregion


        #region Constructors
        public EntityFrameworkProvider(string connectionString)
        {
            var context = new BaseContext(connectionString);
            this._context = context;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="context"></param>
        public EntityFrameworkProvider(BaseContext context)
        {
            this._context = context;
        }
        #endregion


        #region IProvider implementation
        public Func<Type, bool> IsEntityType { get { return Context.IsEntityType; } set { this._context.IsEntityType = value; } }

        /// <summary>
        ///     Set the user Id to use when updating ICreateable and IModifyable entities
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <param name="user"></param>
        public void SetCurrentUser<TId>(TId user)
        {
            this._currentUser = user;
        }

        /// <summary>
        ///     Get the current user assigned to this context
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        public TId GetCurrentUser<TId>()
        {
            return (TId)this._currentUser;
        }

        /// <summary>
        ///     Adds an entity to the context without saving.
        /// </summary>
        /// <param name="entity"></param>
        public void Add<TEntity, TId>(TEntity entity) where TEntity : class, new()
        {
            if (this.Context.Entry(entity).State != EntityState.Detached)
                return;
            var createable = (entity as ICreateable<TId>);
            if (createable != null)
            {
                if (createable.CreatedBy == null || createable.CreatedBy.Equals(default(TId)))
                    createable.CreatedBy = GetCurrentUser<TId>();
                if (createable.DateCreatedUtc == default(DateTime))
                    createable.DateCreatedUtc = DateTime.UtcNow;
            }
            GetDbSet<TEntity>().Add(entity);
        }

        /// <summary>
        ///     Delete an entity.
        /// </summary>
        /// <param name="entity"></param>
        public void Delete<TEntity>(TEntity entity) where TEntity : class, new()
        {
            GetDbSet<TEntity>().Remove(entity);
            Context.SaveChanges();
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
        public IQueryable<TEntity> Query<TEntity>(params Expression<Func<TEntity, object>>[] includes)
            where TEntity : class, new()
        {
            IQueryable<TEntity> returnValue = GetDbSet<TEntity>();
            if (includes == null)
                return returnValue;
            foreach (var include in includes)
            {
                returnValue = returnValue.Include(include);
            }
            return returnValue;
        }

        /// <summary>
        ///     Delete an entity by id.
        /// </summary>
        /// <param name="id"></param>
        public void Delete<TEntity, TId>(TId id) where TEntity : class, new()
        {
            var entity = Get<TEntity, TId>(id);
            GetDbSet<TEntity>().Remove(entity);
            Context.SaveChanges();
        }

        /// <summary>
        ///     Saves all modifications to the context
        /// </summary>
        public void Save()
        {
            SaveAsync(false);
        }

        /// <summary>
        /// Add a new entity instance to the database and commit.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entity"></param>
        /// <param name="safe">Return after entity is saved. If false, attempts to save async, in which case
        /// it is possible to encounter silent exceptions.</param>
        /// <returns></returns>
        public TEntity Insert<TEntity, TId>(TEntity entity, bool safe = true) where TEntity : class, new()
        {
            var createable = (entity as ICreateable<TId>);
            if (createable != null)
            {
                SetCreateableProperties<TEntity, TId>(createable);
            }
            GetDbSet<TEntity>().Add(entity);
            if(safe)
            {
                Context.SaveChanges();
            }
            else
            {
                Context.SaveChangesAsync();
            }
            return entity;
        }

        /// <summary>
        /// Update an entity instance in the database and commit.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entity"></param>
        /// <param name="safe">Return after entity is saved. If false, attempts to save async, in which case
        /// it is possible to encounter silent exceptions.</param>
        /// <returns></returns>
        public TEntity Update<TEntity, TId>(TEntity entity, bool safe = true) where TEntity : class, new()
        {
            var dbEntityEntry = Context.Entry(entity);
            var modifyable = (entity as IModifyable<TId>);
            if (modifyable != null)
            {
                SetModifyableProperties<TEntity, TId>(modifyable);
            }
            if(dbEntityEntry.State == EntityState.Detached)
            {
                GetDbSet<TEntity>().Attach(entity);
            }
            if (safe)
            {
                Context.SaveChanges();
            }
            else
            {
                Context.SaveChangesAsync();
            }
            return entity;
        }


        /// <summary>
        /// Add a new entity instance to the database and commit.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entities"></param>
        /// <param name="safe">Return after entity is saved. If false, attempts to save async, in which case
        /// it is possible to encounter silent exceptions.</param>
        /// <returns></returns>
        public IEnumerable<TEntity> Insert<TEntity, TId>(IEnumerable<TEntity> entities, bool safe = true) where TEntity : class, new()
        {
            var entityArray = entities.ToArray();
            foreach (var entity in entityArray)
            {    
               var createable = (entity as ICreateable<TId>);
                if (createable != null)
                {
                    SetCreateableProperties<TEntity, TId>(createable);
                }
                GetDbSet<TEntity>().Add(entity);
            }
            if (safe)
            {
                Context.SaveChanges();
            }
            else
            {
                Context.SaveChangesAsync();
            }
            return entityArray;
        }

        /// <summary>
        /// Update a collection of entity instances in the database, and commit once.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entities"></param>
        /// <param name="safe">Return after entity is saved. If false, attempts to save async, in which case
        /// it is possible to encounter silent exceptions.</param>
        /// <returns></returns>
        public IEnumerable<TEntity> Update<TEntity, TId>(IEnumerable<TEntity> entities, bool safe = true) where TEntity : class, new()
        {
            var entityArray = entities.ToArray();
            foreach (var entity in entityArray)
            {
                var modifyable = (entity as IModifyable<TId>);
                if (modifyable != null)
                {
                    SetModifyableProperties<TEntity, TId>(modifyable);
                }
                var dbEntityEntry = Context.Entry(entity);
                if (dbEntityEntry.State == EntityState.Detached)
                {
                    GetDbSet<TEntity>().Attach(entity);
                }
            }
            if (safe)
            {
                Context.SaveChanges();
            }
            else
            {
                Context.SaveChangesAsync();
            }
            return entityArray;
        }

        private void SetModifyableProperties<TEntity, TId>(IModifyable<TId> modifyable) where TEntity : class, new()
        {
            modifyable.DateModifiedUtc = DateTime.UtcNow;
            modifyable.ModifiedBy = GetCurrentUser<TId>();
        }

        private void SetCreateableProperties<TEntity, TId>(ICreateable<TId> createable) where TEntity : class, new()
        {
            if (createable.CreatedBy == null || createable.CreatedBy.Equals(default(TId)))
                createable.CreatedBy = GetCurrentUser<TId>();
            if (createable.DateCreatedUtc == default(DateTime))
                createable.DateCreatedUtc = DateTime.UtcNow;
        }

        /// <summary>
        ///     Saves all modifications to the context
        /// </summary>
        public void SaveAsync(bool async)
        {
            if (async)
                Context.SaveChangesAsync().Start();
            else
                Context.SaveChanges();
        }

        /// <summary>
        /// Set the context’s entry for this instance to EntityState.Modified.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        public void SetDetached<TEntity>(TEntity entity) where TEntity : class
        {
            this._context.Entry(entity).State = EntityState.Detached;
        }

        /// <summary>
        /// Set the context’s entry for this instance to EntityState.Modified.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        public void SetModified<TEntity>(TEntity entity) where TEntity : class
        {
            this._context.Entry(entity).State = EntityState.Modified;
            if (entity is IModifyable<TEntity>)
                (entity as IModifyable<TEntity>).DateModifiedUtc = DateTime.UtcNow;
        }

        /// <summary>
        /// Set the context’s entry for this instance to EntityState.Modified.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        public void SetAdded<TEntity>(TEntity entity) where TEntity : class
        {
            this._context.Entry(entity).State = EntityState.Added;
            if (entity is ICreateable<TEntity>)
                (entity as IModifyable<TEntity>).DateModifiedUtc = DateTime.UtcNow;
        }


        #endregion


        #region IEntityProvider Members
        /// <summary>
        ///     Dispose of this class and cleanup the DbContext
        /// </summary>
        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion


        private void dispose(bool disposing)
        {
            if (this._disposed)
                return;
            if (disposing)
            {
                try
                {
                    if (Context == null)
                        return;
                    Context.Dispose();
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
        ///     Get the DbSet for a specific entity type
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public DbSet<TEntity> GetDbSet<TEntity>() where TEntity : class, new()
        {
            return (DbSet<TEntity>)this._dbSets.GetOrAdd(typeof(TEntity), x => Context.Set<TEntity>());
        }
    }
}