using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IExchange
    {
        bool CanGetClosedOrders { get; }

        string Name { get; }
        PercentageValue TakerFeePercentage { get; }
        PercentageValue MakerFeePercentage { get; }

        Task<BalanceResult> GetCurrentBalance(AssetPair assetPair);

        Task<List<FullOrder>> GetOpenOrders();
        Task<List<FullOrder>> GetClosedOrders(GetOrderArgs args = null);
        Task<CancelOrderResult> CancelOrder(OrderId id);

        /// <summary>
        /// Withdraw fiat currency to bank account
        /// </summary>
        /// <param name="amount">Amount to withdraw</param>
        /// <param name="currency">Currency code</param>
        /// <param name="account">Account identifier</param>
        Task<WithdrawFiatResult> WithdrawFundsToBankAccount(decimal amount, string currency, string account);

        /// <summary>
        /// Withdraw cryptocurrency to given address
        /// </summary>
        /// <param name="amount">Amount to withdraw</param>
        /// <param name="currency">Currency code</param>
        /// <param name="address">Address</param>
        Task<WithdrawCryptoResult> WithdrawCryptoToAddress(decimal amount, string currency, string address);

        Task<PaymentMethodResult> GetPaymentMethods();
        Task<FullOrder> GetOrderInfo(OrderId id);

        Task<IOrderBook> GetOrderBook(AssetPair assetPair);
        Task<MinimalOrder> PlaceImmediateBuyOrder(AssetPair assetPair, PriceValue limitPricePerUnit, PriceValue maxVolume);
        Task<MinimalOrder> PlaceImmediateSellOrder(AssetPair assetPair, PriceValue minLimitPrice, PriceValue volume);
    }

    public class GetOrderArgs
    {
        public AssetPair AssetPair { get; set; }
        public DateTime? StartUtc { get; set; }
        public DateTime? EndUtc { get; set; }
    }

    public class GenericResult
    {
        public bool WasCancelled { get; set; }
        public string Error { get; set; }
    }

    public class CancelOrderResult : GenericResult
    {
    }

    public class WithdrawFiatResult : GenericResult
    {
        public string ReferenceId { get; set; }
    }

    public class WithdrawCryptoResult : GenericResult
    {
        public string ReferenceId { get; set; }
    }

    public class PaymentMethodResult
    {
        public List<PaymentMethod> Methods { get; set; } = new List<PaymentMethod>();
    }
}
