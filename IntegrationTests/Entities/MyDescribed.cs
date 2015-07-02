using System;
using Funcular.Ontology.Archetypes;

namespace Funcular.DataProviders.IntegrationTests.Entities 
{
	public partial class MyDescribed : Described<string>
	{
		public Int32? MyNullableIntProperty { get; set; }
		public Boolean MyBoolProperty { get; set; }
		public String MyTextProperty { get; set; }
	}
}
