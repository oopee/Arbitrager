using System;
using System.Collections.Generic;

namespace DatabaseAccess.Entities
{
    // About storing decimal into SQLite with EF: https://github.com/aspnet/EntityFrameworkCore/issues/7232

    public class DbTransaction : DbEntityBase
    {
        /// <summary>
        /// External identifier for the order
        /// </summary>
        public string ExtOrderId { get; set; }

        /// <summary>
        /// Time when this tranaction took place
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Easily readable textual representation on what happened
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Name of the Source Bank or Exchange. Possibly even a wallet Id.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Name of the Target Bank or Exchange. Possibly even a wallet Id.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Source Asset code this transaction transfers assets from. 
        /// </summary>
        public string SourceAsset { get; set; }

        /// <summary>
        /// Target Asset code this transaction transfers assets to.
        /// </summary>
        public string TargetAsset { get; set; }

        /// <summary>
        /// Amount substracted -- in SourceAsset -- from the Source. Includes the SourceFee.
        /// </summary>
        public decimal SourceSentAmount { get; set; }

        /// <summary>
        /// The feet that is substracted from SourceSentAmount before converting to TargetAsset.
        /// </summary>
        public decimal SourceFee { get; set; }

        /// <summary>
        /// Base asset used related to UnitPrice.
        /// </summary>
        public string BaseAsset { get; set; }

        /// <summary>
        /// Quote asset used related to UnitPrice.
        /// </summary>
        public string QuoteAsset { get; set; }

        /// <summary>
        /// Conversion ratio between Base/Quote asset pair. 
        /// Tells how much QuoteAsset you must pay to get single BaseAsset.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Amount substracted -- in TargetAsset -- from the value being added to Target after conversion.
        /// </summary>
        public decimal TargetFee { get; set; }

        /// <summary>
        /// Actual amount received after conversion and TargetFee.
        /// </summary>
        public decimal TargetReceivedAmount { get; set; }
    }
}
