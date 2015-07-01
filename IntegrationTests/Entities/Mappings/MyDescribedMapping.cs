using System.Data.Entity.ModelConfiguration;

namespace Funcular.DataProviders.IntegrationTests.Entities.Mappings 
{
	/* This file was created by a generator; do not edit it directly. In order to add
	 *	relationships and navigation properties, use the corresponding .partial.cs file. */
	public partial class MyDescribedMapping : EntityTypeConfiguration<MyDescribed>
	{
		public MyDescribedMapping()
		{
			HasKey(item => item.Id);
			ToTable("MyDescribed");
			initialize();
		}
	}
}
