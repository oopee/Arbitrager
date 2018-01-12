using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Common.Simulation
{
    public class SimulatedExchangeBase : IExchange
    {
        protected InMemoryOrderStorage m_orderStorage = new InMemoryOrderStorage();

        public string Name { get; private set; }

        public decimal TakerFeePercentage { get; set; }
        public decimal MakerFeePercentage { get; set; }

        public decimal BalanceEur { get; set; }
        public decimal BalanceEth { get; set; }

        public SimulatedExchangeBase(string name)
        {
            Name = name;
        }

        public async Task<CancelOrderResult> CancelOrder(OrderId id)
        {
            await Task.Yield();
            return m_orderStorage.CancelOrder(id);
        }

        public async Task<List<FullOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            await Task.Yield();
            return m_orderStorage.GetClosedOrders(args);
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

        public async Task<List<FullOrder>> GetOpenOrders()
        {
            await Task.Yield();
            return m_orderStorage.GetOpenOrders();
        }

        public async Task<FullOrder> GetOrderInfo(OrderId id)
        {
            await Task.Yield();
            return m_orderStorage.GetOrder(id);
        }

        public Task<PaymentMethodResult> GetPaymentMethods()
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

    public class InMemoryOrderStorage
    {
        public List<SimulatedOrder> Orders { get; set; } = new List<SimulatedOrder>();

        public CancelOrderResult CancelOrder(OrderId id)
        {
            var order = GetOrder(id);
            if (order == null)
            {
                return new CancelOrderResult()
                {
                    Error = "Order not found",
                    WasCancelled = false
                };
            }

            order.State = OrderState.Cancelled;

            return new CancelOrderResult()
            {
                WasCancelled = true
            };
        }

        public List<FullOrder> GetClosedOrders(GetOrderArgs args = null)
        {
            IEnumerable<FullOrder> orders = Orders.Where(x => x.Order.State != OrderState.Open).Select(x => x.Order);
            if (args != null)
            {
                if (args.StartUtc != null)
                {
                    orders = orders.Where(x => x.OpenTime == null || args.StartUtc <= x.OpenTime);
                }

                if (args.EndUtc != null)
                {
                    orders = orders.Where(x => x.OpenTime == null || x.OpenTime <= args.EndUtc);
                }
            }

            return orders.ToList();
        }

        public List<FullOrder> GetOpenOrders()
        {
            return Orders.Where(x => x.Order.State == OrderState.Open).Select(x => x.Order).ToList();
        }

        public FullOrder GetOrder(OrderId id)
        {
            return Orders.Where(x => x.Order.Id == id).FirstOrDefault()?.Order;
        }
    }

    public class SimulatedOrder
    {
        public FullOrder Order { get; set; }

        public SimulatedOrder(FullOrder order)
        {
            Order = order;
        }
    }
}
