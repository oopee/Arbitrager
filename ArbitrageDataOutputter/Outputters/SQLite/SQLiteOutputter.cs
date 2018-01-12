using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using Dapper;

namespace ArbitrageDataOutputter
{
    public class SQLiteOutputter : AbstractArbitrageDataOutputter
    {
        public string SQLiteFile { get; private set; }

        public SQLiteOutputter(IArbitrageDataSource source, string sqliteFile)
            : base(source)
        {
            SQLiteFile = sqliteFile;
        }

        public override Task Initialize()
        {
            EnsureDatabaseExists();
            return Task.FromResult(0);
        }

        protected override Task OutputData(ArbitrageDataPoint info)
        {
            using (var db = GetConnection())
            {
                string insertSQL = @"INSERT INTO [ArbitrageDataRows]
                                     ([TimeStamp], [BestBid], [BestAsk], [MaxNegativeSpreadPercentage], [MaxNegativeSpreadEur], [FiatLimit], [MaxProfitEur], [MaxProfitPercentage])
                                     VALUES (@TimeStamp, @BestBid, @BestAsk, @MaxNegativeSpreadPercentage, @MaxNegativeSpreadEur, @FiatLimit, @MaxProfitEur, @MaxProfitPercentage)";
                var result = db.Execute(insertSQL, info);
            }

            return Task.FromResult(0);
        }

        protected override void OnStarted()
        {
            Console.WriteLine($"Started outputting data to SQLite database {SQLiteFile}");
        }

        protected override void OnStopped()
        {
            Console.WriteLine($"Stopped outputting data to SQLite database {SQLiteFile}");
        }

        private SqliteConnection GetConnection()
        {
            var connectionString = "Data Source=" + SQLiteFile;
            var connection = new SqliteConnection(connectionString);
            connection.Open();

            return connection;
        }

        private void EnsureDatabaseExists()
        {
            if (System.IO.File.Exists(SQLiteFile))
            {
                return;
            }

            CreateDatabase();
        }
        
        private void CreateDatabase()
        {
            using (var db = GetConnection())
            {
                var createCommand = db.CreateCommand();
                createCommand.CommandText =
                @"
                CREATE TABLE ArbitrageDataRows (
                    Id INTEGER PRIMARY KEY,
                    TimeStamp DATETIME2 NOT NULL,
                    BestAsk DECIMAL,
                    BestBid DECIMAL,
                    MaxNegativeSpreadPercentage DECIMAL,
                    MaxNegativeSpreadEur DECIMAL,
                    FiatLimit DECIMAL,
                    MaxProfitEur DECIMAL,
                    MaxProfitPercentage DECIMAL
                )
                ";
                createCommand.ExecuteNonQuery();
            }
        }
    }
}
