using System;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess
{
    public class DatabaseAccess : Interface.IDatabaseAccess
    {
        private string m_dbNameOverride;

        public DatabaseAccess(string dbNameOverride = null)
        {
            m_dbNameOverride = dbNameOverride;
        }

        private async Task GetContext(Func<DbContext, Task> contextAction)
        {
            // Create the DbContext
            using (var db = new DbContext(m_dbNameOverride))
            {
                // Ensure the database is up and running
                db.Database.Migrate();

                // Run action
                await contextAction(db);
            }
        }

        public async Task ResetDatabase()
        {
            // Special case as we don't want to do the normal initialization this time
            using (var db = new DbContext(m_dbNameOverride))
            {
                await db.Database.EnsureDeletedAsync();
            }
        }

        public async Task TestAsync()
        {
            await GetContext(async db =>
            {
                db.Logs.Add(new Interface.Entities.LogLine()
                {
                    Message = "Test method called",
                    Type = Interface.Entities.LogLine.LogType.Test
                });
                db.Logs.Add(new Interface.Entities.LogLine()
                {
                    Message = "Test method called again",
                    Type = Interface.Entities.LogLine.LogType.Test,
                    Items = new System.Collections.Generic.List<Interface.Entities.LogItem>()
                    {
                        new Interface.Entities.LogItem() { ItemNumber = 10 },
                        new Interface.Entities.LogItem() { ItemNumber = 20 },
                        new Interface.Entities.LogItem() { ItemNumber = 30 }
                    }
                });
                await db.SaveChangesAsync();
            });
        }
    }
}
