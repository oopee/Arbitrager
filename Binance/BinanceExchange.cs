using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Binance
{
    public class BinanceExchange : Interface.IExchange
    {
        // TODO mikko namespacet..
        Binance.API.Csharp.Client.BinanceClient m_client;
        ILogger m_logger;

        public BinanceConfiguration Configuration { get; private set; }        
        public bool CanGetClosedOrders => true; // TODO mikko
        public string Name => "Binance";
        public PercentageValue TakerFeePercentage => PercentageValue.FromPercentage(0.1m); // 0.1%
        public PercentageValue MakerFeePercentage => PercentageValue.FromPercentage(0.1m); // 0.1%

        public BinanceExchange(BinanceConfiguration configuration, ILogger logger)
        {
            Configuration = configuration;

            m_logger = logger.WithName(GetType().Name);
            
            var apiClient = new Binance.API.Csharp.Client.ApiClient(configuration.Key, configuration.Secret);
            m_client = new Binance.API.Csharp.Client.BinanceClient(apiClient);
        }
        
        public Task<CancelOrderResult> CancelOrder(OrderId id)
        {
            throw new NotImplementedException();
        }

        public Task<List<FullOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            throw new NotImplementedException();
        }

        public Task<BalanceResult> GetCurrentBalance()
        {
            throw new NotImplementedException();
        }

        public Task<List<FullOrder>> GetOpenOrders()
        {
            throw new NotImplementedException();
        }

        public Task<IOrderBook> GetOrderBook()
        {
            throw new NotImplementedException();
        }

        public Task<FullOrder> GetOrderInfo(OrderId id)
        {
            throw new NotImplementedException();
        }

        public Task<PaymentMethodResult> GetPaymentMethods()
        {
            throw new NotImplementedException();
        }

        public Task<MinimalOrder> PlaceImmediateBuyOrder(PriceValue limitPricePerUnit, PriceValue maxVolume)
        {
            throw new NotImplementedException();
        }

        public Task<MinimalOrder> PlaceImmediateSellOrder(PriceValue minLimitPrice, PriceValue volume)
        {
            throw new NotImplementedException();
        }

        public Task<WithdrawCryptoResult> WithdrawCryptoToAddress(decimal amount, string currency, string address)
        {
            throw new NotImplementedException();
        }

        public Task<WithdrawFiatResult> WithdrawFundsToBankAccount(decimal amount, string currency, string account)
        {
            throw new NotImplementedException();
        }
    }
}
