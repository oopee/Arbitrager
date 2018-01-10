using System;
using System.Linq;
using DatabaseAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess
{
    // https://medium.com/@yostane/entity-framework-core-and-sqlite-getting-started-on-macos-and-visual-studio-code-27f3830db77c
    // https://medium.com/@yostane/entity-framework-core-and-sqlite-database-migration-using-vs2017-macos-28812c64e7ef

    // https://docs.microsoft.com/en-us/ef/core/modeling/relationships

    public class DbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        /*
         *  Tables
         */

        public DbSet<DbTransaction> Transactions { get; set; }

        /*
         *  Logic 
         */

        public static readonly string DATABASE_FILE_NAME_FORMAT = "./arbitrager_{0}.sqlite";

        private string m_configuration;

        public DbContext(string configuration)
        {
            m_configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var fileName = string.Format(DATABASE_FILE_NAME_FORMAT, m_configuration);
            optionsBuilder.UseSqlite("Filename=" + fileName);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var allTypes = System.Reflection.Assembly.GetAssembly(GetType()).GetTypes();
            var baseType = typeof(Configurations.DbConfigurationBase);
            var cfgTypes = allTypes.Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t)).ToList();
            foreach (var t in cfgTypes)
            {
                var i = (Configurations.DbConfigurationBase)t.GetConstructor(new Type[] { }).Invoke(new object[] { });
                i.Configure(modelBuilder);
            }
        }
    }

    // For the migrations
    public class DesignTimeContextCreator : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<DbContext>
    {
        public DbContext CreateDbContext(string[] args)
        {
            return new DbContext("migrations");
        }
    }
}
