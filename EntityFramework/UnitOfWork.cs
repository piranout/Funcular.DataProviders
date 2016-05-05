using System.Collections.Generic;
using Funcular.DataProviders.EntityFramework.SqlServer;

namespace Funcular.DataProviders.EntityFramework
{
    public class UnitOfWork : IContextDisposer, IUnitOfWork
    {
        private readonly ICollection<BaseContext> _contexts = new List<BaseContext>();

        #region Implementation of IDisposable

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            foreach (var context in Contexts)
                context.Dispose();
        }

        #endregion

        #region Implementation of IContextDisposer

        public ICollection<BaseContext> Contexts => _contexts;

        #endregion
    }
}