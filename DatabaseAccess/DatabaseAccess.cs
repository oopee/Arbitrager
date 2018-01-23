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
        
        public async Task<int> StoreArbitrageInfo(int? existingArbitrageId, ArbitrageInfo info)
        {
            Guard(info != null, "Tried to store NULL arbitrage");
            return await GetContext(async db =>
            {
                // Check which DbArbitrage to use
                DbArbitrage a;
                if (existingArbitrageId.HasValue)
                {
                    // Existing Id given --> load from database
                    a = await db.Arbitrages.SingleOrDefaultAsync(x => x.Id == existingArbitrageId.Value);
                    Guard(a != null, "Tried to store non-existing arbitrage with Id: " + existingArbitrageId.Value);
                }
                else
                {
                    // No existing Id given -> create new
                    a = new DbArbitrage();
                }

                // Store values
                a.BaseAsset = info.AssetPair.Base.Name;
                a.QuoteAsset = info.AssetPair.Quote.Name;
                a.BuyingExchange = info.BuyerName;
                a.SellingExchange = info.SellerName;
                a.MaxNegativeSpreadPercentage = DbPercentageValue.FromDomain(info.MaxNegativeSpreadPercentage);
                a.MaxNegativeSpread = DbPriceValue.FromDomain(info.MaxNegativeSpread);
                a.BaseCurrencyBalance = DbPriceValue.FromDomain(info.BaseCurrencyBalance);
                a.QuoteCurrencyBalance = DbPriceValue.FromDomain(info.QuoteCurrencyBalance);
                a.MaxBaseCurrencyAmountToArbitrage = DbPriceValue.FromDomain(info.MaxBaseCurrencyAmountToArbitrage);
                a.MaxQuoteCurrencyAmountToSpend = DbPriceValue.FromDomain(info.MaxQuoteCurrencyAmountToSpend);
                a.MaxQuoteCurrencyToEarn = DbPriceValue.FromDomain(info.MaxQuoteCurrencyToEarn);
                a.MaxQuoteCurrencyProfit = DbPriceValue.FromDomain(info.MaxQuoteCurrencyProfit);
                a.MaxProfitPercentage = DbPercentageValue.FromDomain(info.MaxProfitPercentage);
                a.MaxBuyFee = DbPriceValue.FromDomain(info.MaxBuyFee);
                a.MaxSellFee = DbPriceValue.FromDomain(info.MaxSellFee);
                a.EstimatedAvgBuyUnitPrice = DbPriceValue.FromDomain(info.EstimatedAvgBuyUnitPrice);
                a.EstimatedAvgSellUnitPrice = DbPriceValue.FromDomain(info.EstimatedAvgSellUnitPrice);
                a.EstimatedAvgNegativeSpread = DbPriceValue.FromDomain(info.EstimatedAvgNegativeSpread);
                a.EstimatedAvgNegativeSpreadPercentage = DbPercentageValue.FromDomain(info.EstimatedAvgNegativeSpreadPercentage);
                a.BestBuyPrice = DbPriceValue.FromDomain(info.BestBuyPrice);
                a.BestSellPrice = DbPriceValue.FromDomain(info.BestSellPrice);
                a.BuyLimitPricePerUnit = DbPriceValue.FromDomain(info.BuyLimitPricePerUnit);
                a.IsBaseCurrencyBalanceSufficient = info.IsBaseCurrencyBalanceSufficient;
                a.IsQuoteCurrencyBalanceSufficient = info.IsQuoteCurrencyBalanceSufficient;
                a.IsProfitable = info.IsProfitable;

                // Finally save changes return the arbitrage id
                await db.SaveChangesAsync();
                return a.Id;
            });
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
                        UnitPrice = transaction.LimitPrice?.Value ?? 0m,
                        Source = transaction.Exchange,
                        Target = transaction.Exchange
                };

                    // Then do type dependent stuff
                    if (transaction.Side == OrderSide.Buy)
                    {
                        // Buying: Giving out QuoteAsset to receive BaseAsset
                        tx.SourceAsset = transaction.QuoteAsset.Name;
                        tx.TargetAsset = transaction.BaseAsset.Name;
                        tx.SourceSentAmount = transaction.CostIncludingFee.Value;// TODO: does Krakens Cost include Fee or not?
                        tx.SourceFee = transaction.Fee.Value;
                        tx.TargetFee = 0.0m;
                        tx.TargetReceivedAmount = transaction.FilledVolume.Value;
                    }
                    else // Sell 
                    {
                        // Selling: Giving out BaseAsset to receive QuoteAsset
                        tx.SourceAsset = transaction.BaseAsset.Name;
                        tx.TargetAsset = transaction.QuoteAsset.Name;
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
