using System;
using System.Threading.Tasks;

namespace DatabaseAccess
{
    public class DatabaseAccess : Interface.IDatabaseAccess
    {
        public DatabaseAccess()
        {
        }

        private async Task GetContext(Func<DbContext, Task> contextAction)
        {
            // Create the DbContext
            using (var db = new DbContext())
            {
                // Ensure the database is up and running
                await db.Database.EnsureCreatedAsync();

                // Run action
                await contextAction(db);
            }
        }

        public async Task ResetDatabase()
        {
            await GetContext(db => db.Database.EnsureDeletedAsync());
        }

        public async Task TestAsync()
        {
            await GetContext(async db =>
            {
                db.Logs.Add(new Interface.Entities.LogLine()
                {
                    Message = "Test method called"
                });
                db.Logs.Add(new Interface.Entities.LogLine()
                {
                    Message = "Test method called again",
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
