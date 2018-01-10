using System;
using System.Threading.Tasks;
using DatabaseAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DatabaseAccess
{
    public class DatabaseAccess : Interface.IDatabaseAccess
    {
        private string m_configurationName;

        public DatabaseAccess(string configurationName)
        {
            m_configurationName = configurationName;
        }

        private async Task GetContext(Func<DbContext, Task> contextAction)
        {
            // Create the DbContext
            using (var db = new DbContext(m_configurationName))
            {
                // Ensure the database is up and running
                // db.Database.Migrate();
                await db.Database.EnsureCreatedAsync();

                // Run action
                await contextAction(db);
            }
        }

        public async Task ResetDatabase()
        {
            // Special case as we don't want to do the normal initialization this time
            using (var db = new DbContext(m_configurationName))
            {
                await db.Database.EnsureDeletedAsync();
            }
        }

        public async Task TestAsync()
        {
            await GetContext(async db =>
            {
                db.Logs.Add(new DbLogLine()
                {
                    Message = "Test method called",
                    Type = DbLogLine.LogType.Test
                });
                db.Logs.Add(new DbLogLine()
                {
                    Message = "Test method called again",
                    Type = DbLogLine.LogType.Test,
                    Items = new System.Collections.Generic.List<DbLogItem>()
                    {
                        new DbLogItem() { ItemNumber = 10 },
                        new DbLogItem() { ItemNumber = 20 },
                        new DbLogItem() { ItemNumber = 30 }
                    }
                });
                await db.SaveChangesAsync();
            });
        }
    }
}
