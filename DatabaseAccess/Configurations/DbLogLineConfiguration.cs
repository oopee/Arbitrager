using System;
using DatabaseAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatabaseAccess.Configurations
{
    public class DbLogLineConfiguration : ConfigurationBase<DbLogLine>
    {
        protected override void DoConfigure(EntityTypeBuilder<DbLogLine> b)
        {
            b.HasMany(x => x.Items)
             .WithOne(x => x.LogLine)
             .HasForeignKey(x => x.LogLineId);
        }
    }
}
