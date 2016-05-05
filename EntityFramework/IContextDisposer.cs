using System;
using System.Collections.Generic;
using System.Linq;
using Funcular.DataProviders.EntityFramework.SqlServer;

namespace Funcular.DataProviders.EntityFramework
{
    public interface IContextDisposer : IDisposable
    {
        ICollection<BaseContext> Contexts { get; }
    }
}