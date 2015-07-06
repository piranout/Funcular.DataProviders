using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Funcular.DataProviders.IntegrationTests.Entities
{
    public partial class TransactionItem
    {
        private readonly List<TransactionItemAmendment> _modifications = new List<TransactionItemAmendment>();

        public List<TransactionItemAmendment> Modifications { get { return this._modifications; } }
    }
}
