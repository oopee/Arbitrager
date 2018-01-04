using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IArbitrager
    {
        IBuyer Buyer { get; }
        ISeller Seller { get; }
        Task<Status> GetStatus(bool includeBalance);
        Task<AccountsInfo> GetAccountsInfo();
    }

    public class AccountsInfo
    {
        public List<AccountInfo> Accounts { get; set; } = new List<AccountInfo>();

        public override string ToString()
        {
            return string.Join("\n\n", Accounts);
        }
    }

    public class AccountInfo
    {
        public string Name { get; set; }
        public BalanceResult Balance { get; set; }
        public List<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine();
            b.AppendLine("ACCOUNT");
            b.AppendLine("\t{0}", Name);
            b.AppendLine("\tBalance:");
            b.AppendLine("\t\tEUR: {0}", Balance.Eur);
            b.AppendLine("\t\tETH: {0}", Balance.Eth);
            b.AppendLine();
            b.AppendLine("\tPayment methods:");
            if (PaymentMethods.Count > 0)
            {
                b.AppendLine("\t\t{0}", string.Join("\n\t\t", PaymentMethods.Select(x => x.Name)));
            }
            else
            {
                b.AppendLine("\t\tNone");
            }

            return b.ToString();
        }
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

        public decimal? CashLimit { get; set; } = null;

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

            if (CashLimit.HasValue)
            {
                AddProfitCalculation(b, CashLimit.Value);
            }

            return b.ToString();
        }

        private void AddProfitCalculation(StringBuilder b, decimal cashlimit)
        {
            // First check how much ETH can we buy for the cash limit.
            // We have to calculate this first in case the bids we have (top 1 or top 50)
            // do not cover the whole amount we are willing to buy from other exchange
            decimal ethTotalBids = Seller.Bids.Bids.Sum(x => x.VolumeUnits);

            // Use all money until cash limit or available eth count is reached,
            // starting from the best ask
            decimal ethCount = 0;
            decimal moneySpent = 0;
            int askNro = 0;
            while (moneySpent < cashlimit && ethCount < ethTotalBids && Buyer.Asks.Asks.Count > askNro)
            {
                var orders = Buyer.Asks.Asks[askNro];

                var maxVolume = Math.Min(ethTotalBids - ethCount, orders.VolumeUnits);
                var maxEursToUseAtThisPrice = orders.PricePerUnit * maxVolume;

                var eursToUse = Math.Min(cashlimit - moneySpent, maxEursToUseAtThisPrice);

                moneySpent += eursToUse;
                ethCount += eursToUse / orders.PricePerUnit;

                ++askNro;
            }

            b.AppendLine();

            string moneySpentSummary = string.Format("{0:0.00}e", moneySpent);
            if (moneySpent < cashlimit)
            {
                moneySpentSummary += " (capped, no more bids at other exhcange)";
            }

            b.AppendLine("\t\tCash limit:\t\t\t{0}", moneySpentSummary);
            b.AppendLine("\t\tETH available to buy:\t\t{0:0.0000}", ethCount);

            // How much can this ETH be sold for at other exchange
            decimal ethLeftToSell = ethCount;
            int bidNro = 0;
            decimal moneyEarned = 0;
            while (ethLeftToSell > 0 && Seller.Bids.Bids.Count > bidNro)
            {
                var orders = Seller.Bids.Bids[bidNro];
                var maxEthToSellAtThisPrice = orders.VolumeUnits;
                var ethToSell = Math.Min(ethLeftToSell, maxEthToSellAtThisPrice);

                moneyEarned += ethToSell * orders.PricePerUnit;
                ethLeftToSell -= ethToSell;

                ++bidNro;
            }

            decimal profit = moneyEarned - moneySpent;
            decimal profitAfterTax = profit * 0.7m;

            b.AppendLine("\t\tETH value at other exchange:\t{0:0.00}e", moneyEarned);
            b.AppendLine("\t\tProfit:\t\t\t\t{0:0.00}e ({1:0.00}%)", profit, profit / moneySpent * 100.0m);
            b.AppendLine("\t\tProfit after tax:\t\t{0:0.00}e ({1:0.00}%)", profitAfterTax, profitAfterTax / moneySpent * 100.0m);
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

    public class PaymentMethod
    {
        public PaymentMethodId Id { get; set; }
        public string Name { get; set; }
        public string Currency { get; set; }
        public object RawResult { get; set; }
    }
}
