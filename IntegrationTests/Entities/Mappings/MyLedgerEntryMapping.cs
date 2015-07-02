using System.Data.Entity.ModelConfiguration;

namespace Funcular.DataProviders.IntegrationTests.Entities.Mappings 
{
	/* This file was created by a generator; do not edit it directly. In order to add
	 *	relationships and navigation properties, use the corresponding .partial.cs file. */
	public partial class MyLedgerEntryMapping : EntityTypeConfiguration<MyLedgerEntry>
	{
		public MyLedgerEntryMapping()
		{
			HasKey(item => item.Id);
			ToTable("MyLedgerEntry");
			initialize();
		}
	}
}
