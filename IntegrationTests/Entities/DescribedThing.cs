using System;
using Funcular.Ontology.Archetypes;

namespace Funcular.DataProviders.IntegrationTests.Entities 
{
	public partial class DescribedThing : Described<string>
	{
		public Int32? NullableIntProperty { get; set; }
		public Boolean BoolProperty { get; set; }
		public String TextProperty { get; set; }
	}
}
