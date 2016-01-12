#region File info
// *********************************************************************************************************
// Funcular.DataProviders>Funcular.DataProviders>IEntityProvider.cs
// Created: 2015-07-01 3:53 PM
// Updated: 2015-07-06 10:24 AM
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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
#endregion


namespace Funcular.DataProviders.EntityFramework
{
    public interface IEntityProvider
    {
        /// <summary>
        /// Provide a predicate to tell whether a type is one of your entities.
        /// Typically, this will refer to being derived from a common base class,
        /// or to it using your entity namespace.
        /// </summary>
        Func<Type, bool> IsEntityType { get; set; }

        /// <summary>
        ///     Get an entity by its Id
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        TEntity Get<TEntity, TKey>(TKey id) where TEntity : class, new();

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
        IQueryable<TEntity> Query<TEntity>(params Expression<Func<TEntity, object>>[] includes)
            where TEntity : class, new();

        /// <summary>
        ///     Saves all modifications to the context
        /// </summary>
        void Save();

        /// <summary>
        ///     Adds an entity to the context without saving.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entity"></param>
        void Add<TEntity, TId>(TEntity entity) where TEntity : class, new();

        /// <summary>
        ///     Delete an entity.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <param name="entity"></param>
        void Delete<TEntity, TId>(TEntity entity) where TEntity : class, new();

        /// <summary>
        ///     Delete an entity by id.
        /// </summary>
        /// <param name="id"></param>
        void Delete<TEntity, TId>(TId id) where TEntity : class, new();

        /// <summary>
        ///     Dispose of this class and cleanup the DbContext
        /// </summary>
        void Dispose();

        void SetDetached<TEntity>(TEntity entity) where TEntity : class;
        void SetModified<TEntity>(TEntity entity) where TEntity : class;

        /// <summary>
        ///     Set the user Id to use when updating ICreateable and IModifyable entities
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <param name="user"></param>
        void SetCurrentUser<TId>(TId user);

        /// <summary>
        ///     Get the current user assigned to this context
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        TId GetCurrentUser<TId>();

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
        TEntity Insert<TEntity, TId>(TEntity entity, bool safe = true) where TEntity : class, new();

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
        TEntity Update<TEntity, TId>(TEntity entity, bool safe = true) where TEntity : class, new();

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
        IEnumerable<TEntity> Insert<TEntity, TId>(IEnumerable<TEntity> entities, bool safe = true) where TEntity : class, new();

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
        IEnumerable<TEntity> Update<TEntity, TId>(IEnumerable<TEntity> entities, bool safe = true) where TEntity : class, new();

        /// <summary>
        ///     Set the context’s entry for this instance to EntityState.Modified.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        void SetAdded<TEntity>(TEntity entity) where TEntity : class;

        /// <summary>
        /// Runs business logic on Ontology-derived entities and saves changes.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        int SaveChanges<TId>();

        /// <summary>
        /// Runs business logic on Ontology-derived entities and saves changes.
        /// </summary>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        void SaveChangesAsync<TId>();
    }
}