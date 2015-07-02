using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Funcular.DataProviders.IntegrationTests.Entities
{
    public partial class MyLedgerEntry
    {
        private readonly List<MyLedgerEntryModification> _modifications = new List<MyLedgerEntryModification>();

        public List<MyLedgerEntryModification> Modifications { get { return this._modifications; } }
    }
}
