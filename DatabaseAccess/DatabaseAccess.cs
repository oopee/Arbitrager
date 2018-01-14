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
            // TODO: More of this stuff should really come within FullMyOrder
            var EUR = Asset.EUR.Name;
            var ETH = Asset.ETH.Name;

            if (transaction != null)
            {
                Guard(transaction.State == OrderState.Closed, "Tried to store a transaction which wasn't closed");
                Guard(transaction.Side == OrderSide.Buy || transaction.Side == OrderSide.Sell, "Tried to store transaction of unknown type");

                await GetContext(async db =>
                {
                    // Create transaction and setup stuff that doesn't depend on direction
                    var tx = new DbTransaction()
                    {
                        Timestamp = transaction.OpenTime, // <-- TODO: Can we use OpenTime for this?
                        ExtOrderId = transaction.Id.Id,
                        Description = transaction.ToString(),
                        BaseAsset = ETH,
                        QuoteAsset = EUR,
                        UnitPrice = transaction.LimitPrice ?? 0m
                    };

                    // Then do type dependent stuff
                    if (transaction.Side == OrderSide.Buy)
                    {
                        // At the moment buying means buying ETH with EUR at Kraken
                        tx.Source = "Kraken";
                        tx.Target = "Kraken";
                        tx.SourceAsset = EUR;
                        tx.TargetAsset = ETH;
                        tx.SourceSentAmount = transaction.CostIncludingFee; // TODO: does Krakens Cost include Fee or not?
                        tx.SourceFee = transaction.Fee;
                        tx.TargetFee = 0.0m;
                        tx.TargetReceivedAmount = transaction.FilledVolume;
                    }
                    else // Sell 
                    {
                        // At the moment selling means selling ETH for EUR at GDAX
                        tx.Source = "GDAX";
                        tx.Target = "GDAX";
                        tx.SourceAsset = ETH;
                        tx.TargetAsset = EUR;
                        tx.SourceSentAmount = transaction.FilledVolume;
                        tx.SourceFee = 0.0m;
                        tx.TargetFee = transaction.Fee; // TODO: Does GDAX Cost include Fee or not?
                        tx.TargetReceivedAmount = transaction.CostIncludingFee;
                    }

                    db.Transactions.Add(tx);
                    await db.SaveChangesAsync();
                });
            }
        }
    }
}
