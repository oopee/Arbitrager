using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

using GDAXClient;

namespace Gdax
{
    public class SimulatedGdaxSeller : Interface.ISeller
    {
        GdaxSeller m_realSeller;
        ILogger m_logger;

        public string Name => "SimulatedGDAX";
        public decimal TakerFeePercentage => m_realSeller.TakerFeePercentage;
        public decimal MakerFeePercentage => m_realSeller.MakerFeePercentage;

        public decimal BalanceEur { get; set; }
        public decimal BalanceEth { get; set; }

        public SimulatedGdaxSeller(GdaxConfiguration configuration, ILogger logger, bool isSandbox)
        {
            m_logger = logger.WithName(GetType().Name);
            m_realSeller = new GdaxSeller(configuration, logger, isSandbox);
        }

        public Task<IBidOrderBook> GetBids()
        {
            return m_realSeller.GetBids();
        }

        public async Task<BalanceResult> GetCurrentBalance()
        {
            await Task.Yield();

            return new BalanceResult()
            {
                All = new Dictionary<string, decimal>()
                {
                    { "ETH", BalanceEth },
                    { "EUR", BalanceEur }
                },
                Eth = BalanceEth,
                Eur = BalanceEur
            };
        }

        public Task<MyOrder> PlaceMarketSellOrder(decimal volume)
        {
            throw new NotImplementedException();
        }

        public Task<List<FullMyOrder>> GetOpenOrders()
        {
            throw new NotImplementedException();
        }

        public Task<FullMyOrder> GetOrderInfo(OrderId id)
        {
            throw new NotImplementedException();
        }

        public Task<List<FullMyOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            throw new NotImplementedException();
        }

        public Task<CancelOrderResult> CancelOrder(OrderId id)
        {
            throw new NotImplementedException();
        }

        public Task<PaymentMethodResult> GetPaymentMethods()
        {
            throw new NotImplementedException();
        }

        public Task<WithdrawFiatResult> WithdrawFundsToBankAccount(decimal amount, string currency, string account)
        {
            throw new NotImplementedException();
        }

        public Task<WithdrawCryptoResult> WithdrawCryptoToAddress(decimal amount, string currency, string address)
        {
            throw new NotImplementedException();
        }
    }    
}
