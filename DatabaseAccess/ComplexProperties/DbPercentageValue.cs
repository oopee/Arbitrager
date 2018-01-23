using System;
using System.Collections.Generic;
using System.Text;

using Interface;

namespace DatabaseAccess
{
    public class DbPercentageValue
    {
        public decimal PercentageRatio { get; set; }

        public PercentageValue ToDomain() => PercentageValue.FromRatio(this.PercentageRatio);

        public static DbPercentageValue FromDomain(PercentageValue percentageValue) => percentageValue == null ? null : new DbPercentageValue()
        {
            PercentageRatio = percentageValue.Ratio
        };
    }
}
