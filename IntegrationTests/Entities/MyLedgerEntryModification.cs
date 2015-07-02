using System;
using System.Collections.Generic;
using Funcular.Ontology.Archetypes;

namespace Funcular.DataProviders.IntegrationTests.Entities 
{
	public partial class MyLedgerEntryModification : Createable<string>
	{
		public String LedgerEntryId { get; set; }
		public Decimal ItemAmount { get; set; }
		public String Reason { get; set; }
	}
}
