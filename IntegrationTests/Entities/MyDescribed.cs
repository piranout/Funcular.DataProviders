using System;
using Funcular.Ontology.Archetypes;

namespace Funcular.DataProviders.IntegrationTests.Entities 
{
	public partial class MyDescribed : Described<string>
	{
		public new String Id { get; set; }
		public new DateTime DateCreatedUtc { get; set; }
		public new String CreatedBy { get; set; }
		public new DateTime? DateModifiedUtc { get; set; }
		public new String ModifiedBy { get; set; }
		public new String Name { get; set; }
		public new String Label { get; set; }
		public new String Description { get; set; }
		public Int32? MyNullableIntProperty { get; set; }
		public Boolean MyBoolProperty { get; set; }
		public String MyTextProperty { get; set; }
	}
}
