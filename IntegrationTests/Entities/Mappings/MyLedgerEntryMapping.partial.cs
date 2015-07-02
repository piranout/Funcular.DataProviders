using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;

namespace Funcular.DataProviders.IntegrationTests.Entities.Mappings 
{
	/* This file was created by a generator; do not edit it directly. In order to add
	 *	relationships and navigation properties, use the corresponding .partial.cs file. */
	public partial class MyLedgerEntryMapping : EntityTypeConfiguration<MyLedgerEntry>
	{
		protected void initialize()
		{
			// ADD RELATIONSHIPS AND CUSTOM LOGIC HERE
		    HasMany<MyLedgerEntryModification>(x => x.Modifications)
                .WithRequired()
                .HasForeignKey(modification => modification.LedgerEntryId);
		}
        
	}
}
