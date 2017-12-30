using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IArbitrager
    {
        Task<Status> GetStatus(bool includeBalance);
    }

    public class Status
    {
        public Status(BuyerStatus buyer, SellerStatus seller)
        {
            Buyer = buyer;
            Seller = seller;

            if (Buyer != null && Seller != null)
            {
                Difference = new DifferenceStatus()
                {
                    MaxNegativeSpread = 0m,
                    MaxNegativeSpreadPercentage = 0m
                };

                if (Buyer.Asks.Asks.Count > 0 && Seller.Bids.Bids.Count > 0)
                {
                    var negativeSpread = Buyer.Asks.Asks[0].PricePerUnit - Seller.Bids.Bids[0].PricePerUnit;
                    if (negativeSpread < 0)
                    {
                        Difference.MaxNegativeSpread = Math.Abs(negativeSpread);
                        Difference.MaxNegativeSpreadPercentage = Difference.MaxNegativeSpread / Buyer.Asks.Asks[0].PricePerUnit;
                    }
                }
            }
        }

        public BuyerStatus Buyer { get; set; }
        public SellerStatus Seller { get; set; }
        public DifferenceStatus Difference { get; set; }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine();
            b.AppendLine("BUY");
            if (Buyer == null)
            {
                b.AppendLine("\tNULL");
            }
            else
            {
                b.AppendLine("\t{0}", Buyer.Name);
                if (Buyer.Balance != null)
                {
                    b.AppendLine("\tBalance:");
                    b.AppendLine("\t\tEUR: {0}", Buyer.Balance.Eur);
                    b.AppendLine("\t\tETH: {0}", Buyer.Balance.Eth);
                }
                b.AppendLine("\tAsks (best)");
                b.AppendLine("\t\t{0}", string.Join("\n\t\t", Buyer.Asks.Asks.Take(1)));
            }

            b.AppendLine();
            b.AppendLine("SELL");
            if (Seller == null)
            {
                b.AppendLine("\tNULL");
            }
            else
            {
                b.AppendLine("\t{0}", Seller.Name);
                if (Seller.Balance != null)
                {
                    b.AppendLine("\tBalance:");
                    b.AppendLine("\t\tEUR: {0}", Seller.Balance.Eur);
                    b.AppendLine("\t\tETH: {0}", Seller.Balance.Eth);
                }
                /*b.AppendLine("\tChunk");
                b.AppendLine("\t\tMax euros to use: {0}", ChunkEur);
                b.AppendLine("\t\tActual euros to use: {0}", eurToBuy);*/
                b.AppendLine("\tBids (best)");
                b.AppendLine("\t\t{0}", string.Join("\n\t\t", Seller.Bids.Bids.Take(1)));
            }

            b.AppendLine();
            b.AppendLine("DIFFERENCE");            
            if (Difference == null)
            {
                b.AppendLine("\tNULL");
            }
            else
            {
                b.AppendLine("\t\tMax. negative spread: {0}€", Difference.MaxNegativeSpread);
                b.AppendLine("\t\tMax. negative spread: {0}% (of lowest ask)", (Difference.MaxNegativeSpreadPercentage * 100).ToString("0.##"));
            }

            return b.ToString();
        }
    }

    public class BuyerStatus
    {
        public string Name { get; set; }
        public BalanceResult Balance { get; set; }
        public IAskOrderBook Asks { get; set; }
    }

    public class SellerStatus
    {
        public string Name { get; set; }
        public BalanceResult Balance { get; set; }
        public IBidOrderBook Bids { get; set; }
    }

    public class DifferenceStatus
    {
        /// <summary>
        /// Absolute value of negative spead (negative spread -> ask is lower than bid). If spread is positive, then this value is zero.
        /// </summary>
        public decimal MaxNegativeSpread { get; set; }

        /// <summary>
        /// Ratio MaxNegativeSpread / LowestAskPrice.
        /// </summary>
        public decimal MaxNegativeSpreadPercentage { get; set; }
    }
}
