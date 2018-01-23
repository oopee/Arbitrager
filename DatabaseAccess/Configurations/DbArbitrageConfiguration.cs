using System;
using DatabaseAccess.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatabaseAccess.Configurations
{
    public class DbArbitrageConfiguration : ConfigurationBase<DbArbitrage>
    {
        protected override void DoConfigure(EntityTypeBuilder<DbArbitrage> b)
        {
            b.HasMany(x => x.Transactions)
                .WithOne(x => x.Arbitrage)
                .IsRequired();

            b.OwnsOne(x => x.MaxNegativeSpreadPercentage);
            b.OwnsOne(x => x.MaxNegativeSpread);
            b.OwnsOne(x => x.BaseCurrencyBalance);
            b.OwnsOne(x => x.QuoteCurrencyBalance);
            b.OwnsOne(x => x.MaxBaseCurrencyAmountToArbitrage);
            b.OwnsOne(x => x.MaxQuoteCurrencyAmountToSpend);
            b.OwnsOne(x => x.MaxQuoteCurrencyToEarn);
            b.OwnsOne(x => x.MaxQuoteCurrencyProfit);
            b.OwnsOne(x => x.MaxProfitPercentage);
            b.OwnsOne(x => x.MaxBuyFee);
            b.OwnsOne(x => x.MaxSellFee);
            b.OwnsOne(x => x.EstimatedAvgBuyUnitPrice);
            b.OwnsOne(x => x.EstimatedAvgSellUnitPrice);
            b.OwnsOne(x => x.EstimatedAvgNegativeSpread);
            b.OwnsOne(x => x.EstimatedAvgNegativeSpreadPercentage);
            b.OwnsOne(x => x.BestBuyPrice);
            b.OwnsOne(x => x.BestSellPrice);
            b.OwnsOne(x => x.BuyLimitPricePerUnit);
        }
    }
}
