using System;
using System.Collections.Generic;
using Funcular.Ontology.Archetypes;

namespace Funcular.DataProviders.IntegrationTests.Entities 
{
	public partial class TransactionItemAmendment : Createable<string>
	{
		public String TransactionItemId { get; set; }
		public Decimal ItemAmount { get; set; }
		public String Reason { get; set; }
	}
}
