using System;
using System.Collections.Generic;
using Funcular.Ontology.Archetypes;

namespace Funcular.DataProviders.IntegrationTests.Entities 
{
	public partial class MyLedgerEntry : Createable<string>
	{
		public Decimal ItemAmount { get; set; }
		public String ItemText { get; set; }
	}
}
