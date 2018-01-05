using System;
using System.Linq;
using Interface.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess
{
    // https://medium.com/@yostane/entity-framework-core-and-sqlite-getting-started-on-macos-and-visual-studio-code-27f3830db77c
    // https://medium.com/@yostane/entity-framework-core-and-sqlite-database-migration-using-vs2017-macos-28812c64e7ef

    // https://docs.microsoft.com/en-us/ef/core/modeling/relationships

    public class DbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public static readonly string DATABASE_FILE_NAME = "./arbitrager.sqlite";

        private string m_dbFileNameOverride;

        public DbSet<LogLine> Logs { get; set; }
        public DbSet<LogItem> LogItems { get; set; }

        public DbContext(string dbFileNameOverride = null)
        {
            m_dbFileNameOverride = dbFileNameOverride;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var fileName = m_dbFileNameOverride ?? DATABASE_FILE_NAME;
            optionsBuilder.UseSqlite("Filename=" + fileName);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var allTypes = System.Reflection.Assembly.GetAssembly(GetType()).GetTypes();
            var baseType = typeof(Configurations.ConfigurationBase);
            var cfgTypes = allTypes.Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t)).ToList();
            foreach (var t in cfgTypes)
            {
                var i = (Configurations.ConfigurationBase)t.GetConstructor(new Type[] { }).Invoke(new object[] { });
                i.Configure(modelBuilder);
            }
        }
    }
}
