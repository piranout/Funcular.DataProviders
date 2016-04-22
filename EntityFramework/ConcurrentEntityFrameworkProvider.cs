using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using EntityFramework.Utilities;
using Funcular.DataProviders.EntityFramework.SqlServer;
using Funcular.Ontology.Archetypes;

namespace Funcular.DataProviders.EntityFramework
{
    public class ConcurrentEntityFrameworkProvider : EntityFrameworkProvider
    {
        protected readonly string _connectionString;

        protected static ConcurrentDictionary<string, ThreadLocal<BaseContext>> 
            _contextDictionary = new ConcurrentDictionary<string, ThreadLocal<BaseContext>>();

        public override BaseContext Context { get { return _contextDictionary[this._connectionString].Value; } }

        public ConcurrentEntityFrameworkProvider(string connectionString) : this()
        {
            _connectionString = connectionString;
            _contextDictionary.TryAdd(connectionString,
                new ThreadLocal<BaseContext>(() => new BaseContext(connectionString)));
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="context"></param>
        public ConcurrentEntityFrameworkProvider(BaseContext context) 
        {
            throw new NotImplementedException();
        }

        protected ConcurrentEntityFrameworkProvider()
        {
        }
    }
}