using System;
using System.Collections.Generic;
using System.Text;

using Interface;

namespace DatabaseAccess
{
    public class DbPriceValue
    {
        public decimal Value { get; set; }
        public string Asset { get; set; }

        public PriceValue ToDomain() => new PriceValue(this.Value, Interface.Asset.Get(this.Asset));

        public static DbPriceValue FromDomain(PriceValue priceValue) => priceValue == null ? null : new DbPriceValue()
        {
            Value = priceValue.Value,
            Asset = priceValue.Asset.Name
        };
    }
}
