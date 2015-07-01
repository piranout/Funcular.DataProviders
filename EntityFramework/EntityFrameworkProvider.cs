#region File info
// *********************************************************************************************************
// Funcular.DataProviders>Funcular.DataProviders>EntityFrameworkProvider.cs
// Created: 2015-07-01 3:52 PM
// Updated: 2015-07-01 3:56 PM
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

        private bool _disposed;

        public static HashSet<string> EntityAssemblyNames { get { return BaseContext.EntityAssemblyNames; } }

        public BaseContext Context { get { return this._context; } }

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
        ///     Adds an entity to the context without saving.
        /// </summary>
        /// <param name="entity"></param>
        public void Add<TEntity>(TEntity entity) where TEntity : class, new()
        {
            if (Context.Entry(entity)
                .State == EntityState.Detached)
            {
                GetDbSet<TEntity>()
                    .Add(entity);
            }
        }

        /// <summary>
        ///     Delete an entity.
        /// </summary>
        /// <param name="entity"></param>
        public void Delete<TEntity>(TEntity entity) where TEntity : class, new()
        {
            GetDbSet<TEntity>()
                .Remove(entity);
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
            return GetDbSet<TEntity>()
                .Find(id);
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

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    returnValue = returnValue.Include(include);
                }
            }

            return returnValue;
        }

        /// <summary>
        ///     Dispose of this class and cleanup the DbContext
        /// </summary>
        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Delete an entity by id.
        /// </summary>
        /// <param name="id"></param>
        public void Delete<TEntity, TId>(TId id) where TEntity : class, new()
        {
            var entity = Get<TEntity, TId>(id);
            GetDbSet<TEntity>()
                .Remove(entity);
            Context.SaveChanges();
        }

        public void SetDetached<TEntity>(TEntity entity) where TEntity : class
        {
            this._context.Entry(entity)
                .State = EntityState.Detached;
        }

        public void SetModified<TEntity>(TEntity entity) where TEntity : class
        {
            this._context.Entry(entity)
                .State = EntityState.Modified;
            if (entity is IModifyable<TEntity>)
            {
                (entity as IModifyable<TEntity>).DateModifiedUtc = DateTime.UtcNow;
            }
        }

        /// <summary>
        ///     Saves all modifications to the context
        /// </summary>
        public void Save()
        {
            Save(false);
        }

        /// <summary>
        ///     Save the entity. This will create an entity if it doesnt
        ///     exist, otherwise it will update it.
        /// </summary>
        /// <param name="entity"></param>
        public void Save<TEntity>(TEntity entity) where TEntity : class, new()
        {
            if (Context.Entry(entity)
                .State == EntityState.Detached)
            {
                GetDbSet<TEntity>()
                    .Add(entity);
            }
            else
            {
                Context.Entry(entity)
                    .State = EntityState.Modified;
                if (entity is IModifyable<TEntity>)
                {
                    (entity as IModifyable<TEntity>).DateModifiedUtc = DateTime.UtcNow;
                }
            }
            try
            {
                Context.SaveChanges();
            }
            catch (Exception e)
            {
                var sqlException = e.InnerException as SqlException;
                var success = false;
                if (e.ToString()
                    .Contains("VIOLATION OF PRIMARY KEY constraint")
                    || e.ToString()
                        .Contains("Cannot insert duplicate key")
                    || (sqlException != null) && sqlException.ToString()
                        .Contains("duplicate key"))
                {
                    SetModified(entity);
                    if (entity is IModifyable<TEntity>)
                    {
                        (entity as IModifyable<TEntity>).DateModifiedUtc = DateTime.UtcNow;
                    }
                    Context.SaveChanges();
                    success = true;
                }
                Console.WriteLine(e);
                if (!success)
                    throw;
            }
        }

        public void Save<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, new()
        {
            Context.Configuration.AutoDetectChangesEnabled = false;
            var i = 0;
            var saveTimer = Stopwatch.StartNew();
            IEnumerable<TEntity> enumerable = entities as TEntity[] ?? entities.ToArray();
            while (enumerable.Any())
            {
                entities = enumerable.Skip(i);
                i = 1000;
                foreach (var entity in entities.Take(i))
                {
                    if (Context.Entry(entity).State == EntityState.Detached)
                    {
                        GetDbSet<TEntity>()
                            .Add(entity);
                    }
                    else
                    {
                        Context.Entry(entity)
                            .State = EntityState.Modified;
                        if (entity is IModifyable<TEntity>)
                        {
                            (entity as IModifyable<TEntity>).DateModifiedUtc = DateTime.UtcNow;
                        }
                    }
                }
                Context.SaveChanges();
            }
            saveTimer.Stop();
        }

        /// <summary>
        ///     Saves all modifications to the context
        /// </summary>
        public void Save(bool async)
        {
            if (async)
                Context.SaveChangesAsync().Start();
            else
                Context.SaveChanges();
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
            return (DbSet<TEntity>) this._dbSets.GetOrAdd(typeof (TEntity), x => Context.Set<TEntity>());
        }
    }
}