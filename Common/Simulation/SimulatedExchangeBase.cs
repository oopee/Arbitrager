using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Common.Simulation
{
    public abstract class SimulatedExchangeBase : IExchange
    {
        protected InMemoryOrderStorage m_orderStorage = new InMemoryOrderStorage();

        public string Name { get; private set; }

        public PercentageValue TakerFeePercentage { get; set; }
        public PercentageValue MakerFeePercentage { get; set; }

        public PriceValue BalanceEur { get; set; }
        public PriceValue BalanceEth { get; set; }

        public bool CanGetClosedOrders => true;

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
                    { "ETH", BalanceEth.Value },
                    { "EUR", BalanceEur.Value }
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

        public async Task<MinimalOrder> PlaceImmediateBuyOrder(PriceValue price, PriceValue volume)
        {
            var orderBook = await GetOrderBook();

            // Limit order with Immediate or Cancel -> only take orders with unit price is less than 'price' argument
            var orders = Common.DefaultProfitCalculator.GetFromOrderBook(orderBook.Asks.Where(x => x.PricePerUnit <= price), null, volume.Value).ToList();

            var sum = new PriceValue(orders.Select(x => x.PricePerUnit * x.VolumeUnits).DefaultIfEmpty().Sum(), price.Asset);
            var fee = sum * TakerFeePercentage;
            var filledVolume = new PriceValue(orders.Select(x => x.VolumeUnits).DefaultIfEmpty().Sum(), volume.Asset);
            var pricePerUnitWithoutFee = filledVolume == 0 ? sum.AsZero() : sum / filledVolume.Value;
            var pricePerUnitWithFee = filledVolume == 0 ? sum.AsZero() : (sum + fee) / filledVolume.Value;
            var totalCost = sum + fee;

            if (totalCost > BalanceEur)
            {
                throw new Exception("Invalid EUR balance");
            }

            BalanceEur -= totalCost;
            BalanceEth += filledVolume;

            var createTime = TimeService.UtcNow;
            await Task.Delay(10);
            var closeTime = TimeService.UtcNow;

            var newOrder = new Common.Simulation.SimulatedOrder(new FullOrder()
            {
                Volume = volume,
                FilledVolume = filledVolume,
                CostExcludingFee = sum,
                LimitPrice = pricePerUnitWithoutFee,
                State = filledVolume == volume ? OrderState.Closed : OrderState.Cancelled,
                OpenTime = createTime,
                CloseTime = closeTime,
                ExpireTime = null,
                Fee = fee,
                Type = OrderType.Limit,
                Side = OrderSide.Buy,
                Id = new OrderId(Guid.NewGuid().ToString())
            });

            m_orderStorage.Orders.Add(newOrder);

            return new MinimalOrder()
            {
                Id = newOrder.Order.Id,
                Side = newOrder.Order.Side,
            };
        }

        public async Task<MinimalOrder> PlaceImmediateSellOrder(PriceValue minLimitPrice, PriceValue volume)
        {
            if (volume > BalanceEth)
            {
                throw new Exception("Invalid ETH balance");
            }

            var orderBook = await GetOrderBook();

            // Limit order with Immediate or Cancel -> only take orders with unit price more than 'price' argument
            var orders = Common.DefaultProfitCalculator.GetFromOrderBook(orderBook.Bids.Where(x => x.PricePerUnit >= minLimitPrice), null, volume.Value).ToList();

            var sum = new PriceValue(orders.Select(x => x.PricePerUnit * x.VolumeUnits).DefaultIfEmpty().Sum(), minLimitPrice.Asset);
            var fee = sum * TakerFeePercentage;
            var filledVolume = new PriceValue(orders.Select(x => x.VolumeUnits).DefaultIfEmpty().Sum(), volume.Asset);
            var pricePerUnitWithoutFee = filledVolume == 0 ? sum.AsZero() : sum / filledVolume.Value;
            var pricePerUnitWithFee = filledVolume == 0 ? sum.AsZero() : (sum - fee) / filledVolume.Value;
            var totalCost = sum - fee;

            var createTime = TimeService.UtcNow;
            await Task.Delay(10);
            var closeTime = TimeService.UtcNow;

            var newOrder = new Common.Simulation.SimulatedOrder(new FullOrder()
            {
                Volume = volume,
                FilledVolume = filledVolume,
                CostExcludingFee = sum,
                LimitPrice = minLimitPrice,
                State = filledVolume == volume ? OrderState.Closed : OrderState.Cancelled,
                OpenTime = createTime,
                Fee = fee,
                Type = OrderType.Market,
                Side = OrderSide.Sell,
                Id = new OrderId(Guid.NewGuid().ToString()),
                CloseTime = closeTime,
                ExpireTime = null
            });

            m_orderStorage.Orders.Add(newOrder);

            BalanceEur += totalCost;
            BalanceEth -= filledVolume;

            return new MinimalOrder()
            {
                Id = newOrder.Order.Id,
                Side = newOrder.Order.Side,
            };
        }

        public abstract Task<IOrderBook> GetOrderBook();
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
