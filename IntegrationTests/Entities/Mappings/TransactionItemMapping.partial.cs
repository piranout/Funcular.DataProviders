using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;

namespace Funcular.DataProviders.IntegrationTests.Entities.Mappings 
{
	/* This file was created by a generator; do not edit it directly. In order to add
	 *	relationships and navigation properties, use the corresponding .partial.cs file. */
	public partial class TransactionItemMapping : EntityTypeConfiguration<TransactionItem>
	{
		protected void initialize()
		{
			// ADD RELATIONSHIPS AND CUSTOM LOGIC HERE
		    HasMany<TransactionItemAmendment>(x => x.Modifications)
                .WithRequired()
                .HasForeignKey(modification => modification.TransactionItemId);
		}
        
	}
}
