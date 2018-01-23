using System;
using DatabaseAccess.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatabaseAccess.Configurations
{
    public class DbTransactionConfiguration : ConfigurationBase<DbTransaction>
    {
        protected override void DoConfigure(EntityTypeBuilder<DbTransaction> b)
        {
            b.Property(x => x.ArbitrageId).IsRequired(false);
        }
    }
}
