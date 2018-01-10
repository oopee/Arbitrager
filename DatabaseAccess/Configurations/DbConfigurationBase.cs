using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatabaseAccess.Configurations
{
    public abstract class DbConfigurationBase 
    {
        public abstract void Configure(ModelBuilder b);
    }

    public abstract class ConfigurationBase<TEntity> : DbConfigurationBase where TEntity : Entities.DbEntityBase
    {
        public override void Configure(ModelBuilder b)
        {
            DoConfigure(b.Entity<TEntity>());
        }

        protected abstract void DoConfigure(EntityTypeBuilder<TEntity> b);
    }
}
