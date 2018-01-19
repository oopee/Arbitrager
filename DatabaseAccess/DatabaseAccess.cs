using System;
using System.Threading.Tasks;
using Common;
using DatabaseAccess.Entities;
using Interface;
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

        private async Task<T> GetContext<T>(Func<DbContext, Task<T>> contextAction)
        {
            // Create the DbContext
            using (var db = new DbContext(m_configurationName))
            {
                // Ensure the database is up and running
                db.Database.Migrate();

                // Run action
                return await contextAction(db);
            }
        }

        private async Task GetContext(Func<DbContext, Task> contextAction)
        {
            await GetContext<object>(async db =>
            {
                await contextAction(db);
                return null;
            });
        }

        private void Guard(bool check, string msg)
        {
            Interface.Guard.IsTrue(check, "DB: " + msg);
        }

        public async Task ResetDatabase()
        {
            // Special case as we don't want to do the normal initialization this time
            using (var db = new DbContext(m_configurationName))
            {
                await db.Database.EnsureDeletedAsync();
            }
        }

        public async Task StoreTransaction(FullOrder transaction)
        {
            if (transaction != null)
            {
                Guard(transaction.State == OrderState.Closed || transaction.State == OrderState.Cancelled, "Tried to store a transaction which wasn't closed");
                Guard(transaction.Side == OrderSide.Buy || transaction.Side == OrderSide.Sell, "Tried to store transaction of unknown type");

                await GetContext(async db =>
                {
                    // Create transaction and setup stuff that doesn't depend on direction
                    var tx = new DbTransaction()
                    {
                        Timestamp = transaction.OpenTime, // <-- TODO: Can we use OpenTime for this?
                        ExtOrderId = transaction.Id.Id,
                        Description = transaction.ToString(),
                        BaseAsset = transaction.BaseAsset.Name,
                        QuoteAsset = transaction.QuoteAsset.Name,
                        UnitPrice = transaction.LimitPrice?.Value ?? 0m
                    };

                    // Then do type dependent stuff
                    if (transaction.Side == OrderSide.Buy)
                    {
                        tx.Source = transaction.SourceExchange;
                        tx.Target = transaction.TargetExchange;
                        tx.SourceAsset = transaction.SourceAsset.Name;
                        tx.TargetAsset = transaction.TargetAsset.Name;
                        tx.SourceSentAmount = transaction.CostIncludingFee.Value;// TODO: does Krakens Cost include Fee or not?
                        tx.SourceFee = transaction.Fee.Value;
                        tx.TargetFee = 0.0m;
                        tx.TargetReceivedAmount = transaction.FilledVolume.Value;
                    }
                    else // Sell 
                    {
                        tx.Source = transaction.SourceExchange;
                        tx.Target = transaction.TargetExchange;
                        tx.SourceAsset = transaction.SourceAsset.Name;
                        tx.TargetAsset = transaction.TargetAsset.Name;
                        tx.SourceSentAmount = transaction.FilledVolume.Value;
                        tx.SourceFee = 0.0m;
                        tx.TargetFee = transaction.Fee.Value; // TODO: Does GDAX Cost include Fee or not?
                        tx.TargetReceivedAmount = transaction.CostIncludingFee.Value;
                    }

                    db.Transactions.Add(tx);
                    await db.SaveChangesAsync();
                });
            }
        }
    }
}
