using System;
using System.Linq.Expressions;

namespace Funcular.DataProviders.EntityFramework
{
    public class ReferencePropertySetOperation<T, TProp> : IEntityPropertyUpdate where TProp : class
    {
        public T Entity { get; set; }
        public Expression<Func<T, TProp>> Property { get; set; }
        public TProp Value { get; set; }

    }
}