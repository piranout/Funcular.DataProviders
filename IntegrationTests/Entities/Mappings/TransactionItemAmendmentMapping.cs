using System.Data.Entity.ModelConfiguration;

namespace Funcular.DataProviders.IntegrationTests.Entities.Mappings 
{
	/* This file was created by a generator; do not edit it directly. In order to add
	 *	relationships and navigation properties, use the corresponding .partial.cs file. */
	public partial class TransactionItemAmendmentMapping : EntityTypeConfiguration<TransactionItemAmendment>
	{
		public TransactionItemAmendmentMapping()
		{
			HasKey(item => item.Id);
			ToTable("TransactionItemAmendment");
			initialize();
		}
	}
}
