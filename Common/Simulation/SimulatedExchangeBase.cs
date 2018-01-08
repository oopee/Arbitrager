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

        public async Task<List<FullMyOrder>> GetClosedOrders(GetOrderArgs args = null)
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

        public async Task<List<FullMyOrder>> GetOpenOrders()
        {
            await Task.Yield();
            return m_orderStorage.GetOpenOrders();
        }

        public async Task<FullMyOrder> GetOrderInfo(OrderId id)
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

        public List<FullMyOrder> GetClosedOrders(GetOrderArgs args = null)
        {
            IEnumerable<FullMyOrder> orders = Orders.Where(x => x.Order.State != OrderState.Open).Select(x => x.Order);
            if (args != null)
            {
                if (args.StartUtc != null)
                {
                    orders = orders.Where(x => x.StartTime == null || args.StartUtc <= x.StartTime);
                }

                if (args.EndUtc != null)
                {
                    orders = orders.Where(x => x.StartTime == null || x.StartTime <= args.EndUtc);
                }
            }

            return orders.ToList();
        }

        public List<FullMyOrder> GetOpenOrders()
        {
            return Orders.Where(x => x.Order.State == OrderState.Open).Select(x => x.Order).ToList();
        }

        public FullMyOrder GetOrder(OrderId id)
        {
            return Orders.Where(x => x.Order.Id == id).FirstOrDefault()?.Order;
        }
    }

    public class SimulatedOrder
    {
        public FullMyOrder Order { get; set; }

        public SimulatedOrder(FullMyOrder order)
        {
            Order = order;
        }
    }
}
