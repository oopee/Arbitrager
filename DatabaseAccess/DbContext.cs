using System;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess
{
    // https://medium.com/@yostane/entity-framework-core-and-sqlite-getting-started-on-macos-and-visual-studio-code-27f3830db77c
    // https://medium.com/@yostane/entity-framework-core-and-sqlite-database-migration-using-vs2017-macos-28812c64e7ef

    public class DbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public static readonly string DATABASE_FILE_NAME = "./arbitrager.sqlite";

        public DbSet<Interface.Entities.LogLine> Logs { get; set; }
        public DbSet<Interface.Entities.LogItem> LogItems { get; set; }

        protected override void OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=" + DATABASE_FILE_NAME);
        }
    }
}
