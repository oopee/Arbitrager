using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Kraken
{
    public class SimulatedKrakenBuyer : Interface.IBuyer
    {
        ILogger m_logger;
        KrakenBuyer m_realKraken;

        public string Name => "SimulatedKraken";
        public decimal TakerFeePercentage => m_realKraken.TakerFeePercentage;
        public decimal MakerFeePercentage => m_realKraken.MakerFeePercentage;

        public decimal BalanceEur { get; set; }
        public decimal BalanceEth { get; set; }

        public SimulatedKrakenBuyer(KrakenConfiguration configuration, ILogger logger)
        {
            m_logger = logger.WithName(GetType().Name);
            m_realKraken = new KrakenBuyer(configuration, logger);
        }

        public Task<IAskOrderBook> GetAsks()
        {
            return m_realKraken.GetAsks();
        }

        public Task<MyOrder> PlaceImmediateBuyOrder(decimal price, decimal volume)
        {
            throw new NotImplementedException();
        }

        private Task<OrderBook> GetOrderBook()
        {
            throw new NotImplementedException();
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

        public Task<PaymentMethodResult> GetPaymentMethods()
        {
            return Task.FromResult(new PaymentMethodResult());
        }

        public Task<CancelOrderResult> CancelOrder(OrderId id)
        {
            throw new NotImplementedException();
        }

        public Task<List<FullMyOrder>> GetOpenOrders()
        {
            throw new NotImplementedException();
        }

        public Task<List<FullMyOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            throw new NotImplementedException();
        }

        public Task<FullMyOrder> GetOrderInfo(OrderId id)
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
