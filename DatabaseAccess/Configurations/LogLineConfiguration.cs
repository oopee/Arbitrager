using System;
using Interface.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatabaseAccess.Configurations
{
    public class LogLineConfiguration : ConfigurationBase<LogLine>
    {
        protected override void DoConfigure(EntityTypeBuilder<LogLine> b)
        {
            b.HasMany(x => x.Items)
             .WithOne(x => x.LogLine)
             .HasForeignKey(x => x.LogLineId);
        }
    }
}
