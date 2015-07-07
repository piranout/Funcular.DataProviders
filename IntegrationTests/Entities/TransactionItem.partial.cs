using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Funcular.DataProviders.IntegrationTests.Entities
{
    public partial class TransactionItem
    {
        protected readonly List<TransactionItemAmendment> _modifications = new List<TransactionItemAmendment>();

        public virtual List<TransactionItemAmendment> Modifications { get { return this._modifications; } }
    }
}
