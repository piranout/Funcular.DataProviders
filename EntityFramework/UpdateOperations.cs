using System.Collections.Generic;

namespace Funcular.DataProviders.EntityFramework
{
    public class UpdateOperations
    {
        public UpdateOperations()
        {
            SetOperations = new List<IEntityPropertyUpdate>();
        }

        public ICollection<IEntityPropertyUpdate> SetOperations { get; private set; } 
    }
}