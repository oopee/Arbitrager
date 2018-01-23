using System;
using System.Collections.Generic;

namespace DatabaseAccess.Entities
{

    public class DbArbitrage : DbEntityBase
    {
        public string BaseAsset { get; set; }   
        public string QuoteAsset { get; set; }

        public string BuyingExchange { get; set; }
        public string SellingExchange { get; set; }

        public DbPercentageValue MaxNegativeSpreadPercentage { get; set; }
        public DbPriceValue MaxNegativeSpread { get; set; }
        public DbPriceValue BaseCurrencyBalance { get; set; }
        public DbPriceValue QuoteCurrencyBalance { get; set; }

        public DbPriceValue MaxBaseCurrencyAmountToArbitrage { get; set; }
        public DbPriceValue MaxQuoteCurrencyAmountToSpend { get; set; }
        public DbPriceValue MaxQuoteCurrencyToEarn { get; set; }
        public DbPriceValue MaxQuoteCurrencyProfit { get; set; }
        public DbPercentageValue MaxProfitPercentage { get; set; }
        public DbPriceValue MaxBuyFee { get; set; }
        public DbPriceValue MaxSellFee { get; set; }

        public DbPriceValue EstimatedAvgBuyUnitPrice { get; set; }
        public DbPriceValue EstimatedAvgSellUnitPrice { get; set; }
        public DbPriceValue EstimatedAvgNegativeSpread { get; set; }
        public DbPercentageValue EstimatedAvgNegativeSpreadPercentage { get; set; }

        public DbPriceValue BestBuyPrice { get; set; }
        public DbPriceValue BestSellPrice { get; set; }

        public DbPriceValue BuyLimitPricePerUnit { get; set; }

        public bool IsBaseCurrencyBalanceSufficient { get; set; }
        public bool IsQuoteCurrencyBalanceSufficient { get; set; }
        public bool IsProfitable { get; set; }

        public virtual List<DbTransaction> Transactions { get; set; }
    }
}
