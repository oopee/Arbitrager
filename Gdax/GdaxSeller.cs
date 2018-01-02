using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

using GDAXClient;

namespace Gdax
{
    public class GdaxSeller : Interface.ISeller
    {
        GDAXClient.Authentication.Authenticator m_authenticator;
        GDAXClient.GDAXClient m_client;
        ILogger m_logger;

        public string Name => "GDAX";

        public GdaxSeller(GdaxConfiguration configuration, ILogger logger, bool isSandbox)
        {
            m_logger = logger;
            m_authenticator = new GDAXClient.Authentication.Authenticator(configuration.Key, configuration.Signature, configuration.Passphrase);
            m_client = new GDAXClient.GDAXClient(m_authenticator, sandBox: isSandbox);
        }

        public async Task<IBidOrderBook> GetBids()
        {
            var result = await m_client.ProductsService.GetProductOrderBookAsync(GDAXClient.Services.Orders.ProductType.EthEur);

            var orderBook = new OrderBook();
            if (result.Asks.Any())
            {
                orderBook.Asks.AddRange(result.Asks.Select(x => new OrderBookOrder()
                {
                    PricePerUnit = x.First(),
                    VolumeUnits = x.Skip(1).First(),
                    Timestamp = DateTime.UtcNow
                }));
            }

            if (result.Bids.Any())
            {
                orderBook.Bids.AddRange(result.Bids.Select(x => new OrderBookOrder()
                {
                    PricePerUnit = x.First(),
                    VolumeUnits = x.Skip(1).First(),
                    Timestamp = DateTime.UtcNow
                }));
            }

            return orderBook;
        }

        public async Task<BalanceResult> GetCurrentBalance()
        {
            var accounts = await m_client.AccountsService.GetAllAccountsAsync();

            var all = accounts.ToDictionary(x => x.Currency, x => x.Balance);

            return new BalanceResult()
            {
                All = all,
                Eth = all.Where(x => x.Key == "ETH").FirstOrDefault().Value,
                Eur = all.Where(x => x.Key == "EUR").FirstOrDefault().Value
            };
        }

        public async Task<MyOrder> PlaceSellOrder(decimal price, decimal volume)
        {
            var order = await m_client.OrdersService.PlaceLimitOrderAsync(
                GDAXClient.Services.Orders.OrderSide.Sell,
                GDAXClient.Services.Orders.ProductType.EthEur,
                volume,
                price);

            var orderResult = new MyOrder()
            {
                Id = new OrderId(order.Id.ToString()),
                PricePerUnit = order.Price,
                Volume = order.Size,
                Type = OrderType.Sell
            };

            m_logger.Info("GdaxSeller: placed sell order {0}", orderResult);

            return orderResult;
        }

        public async Task<List<FullMyOrder>> GetOpenOrders()
        {
            var orders = await m_client.OrdersService.GetAllOrdersAsync();

            var result = orders.SelectMany(x => x).Select(x => ParseOrder(x)).ToList();
            return result;
        }

        public async Task<FullMyOrder> GetOrderInfo(OrderId id)
        {
            var order = await m_client.OrdersService.GetOrderByIdAsync(id.ToString());
            return ParseOrder(order);
        }

        public Task<List<FullMyOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            throw new NotImplementedException();
        }

        public async Task<CancelOrderResult> CancelOrder(OrderId id)
        {
            var result = await m_client.OrdersService.CancelOrderByIdAsync(id.Id);

            return new CancelOrderResult()
            {
                WasCancelled = result.OrderIds.Select(x => x.ToString()).Contains(id.Id)
            };
        }

        public Task<WithdrawEurResult> WithdrawFundsToBankAccount(decimal eur)
        {
            throw new NotImplementedException();
        }

        public async Task<PaymentMethodResult> GetPaymentMethods()
        {
            var methods = await m_client.PaymentsService.GetAllPaymentMethodsAsync();

            return new PaymentMethodResult()
            {
                Methods = methods.Select(x => new PaymentMethod()
                {
                    Id = new PaymentMethodId(x.Id.ToString()),
                    Name = x.Name,
                    Currency = x.Currency,
                    RawResult = x
                }).ToList()
            };
        }

        private FullMyOrder ParseOrder(GDAXClient.Services.Orders.OrderResponse order)
        {
            return new FullMyOrder()
            {
                Id = new OrderId(order.Id.ToString()),
                Fee = order.Fill_fees,
                OpenTime = order.Created_at,
                StartTime = null,
                ExpireTime = null,
                FilledVolume = order.Filled_size,
                Volume = order.Size,
                PricePerUnit = order.Price,
                Type = ParseSide(order.Side),
                OrderType = ParseOrderType(order.Type),
                State = ParseState(order.Status),
                Cost = 0m // TODO
            };
        }

        private OrderType ParseSide(string side)
        {
            switch (side)
            {
                case "buy": return OrderType.Buy;
                case "sell": return OrderType.Sell;
                default:
                    m_logger.Error("GdaxSeller.ParseSide: unknown value '{0}", side);
                    return OrderType.Unknown;
            }
        }

        private OrderType2 ParseOrderType(string type)
        {
            switch (type)
            {
                case "limit": return OrderType2.Limit;
                default:
                    m_logger.Error("GdaxSeller.ParseOrderType: unknown value '{0}", type);
                    return OrderType2.Unknown;
            }
        }

        private OrderState ParseState(string state)
        {
            switch (state)
            {
                case "open": return OrderState.Open;
                case "done": return OrderState.Closed;
                default:
                    m_logger.Error("GdaxSeller.ParseState: unknown value '{0}", state);
                    return OrderState.Unknown;
            }
        }
    }

    public class GdaxConfiguration
    {
        public string Key { get; set; }
        public string Signature { get; set; }
        public string Passphrase { get; set; }

        public static GdaxConfiguration FromAppConfig()
        {
            return new GdaxConfiguration()
            {
                Key = System.Configuration.ConfigurationManager.AppSettings["GdaxKey"] ?? "",
                Signature = System.Configuration.ConfigurationManager.AppSettings["GdaxSecret"] ?? "",
                Passphrase = System.Configuration.ConfigurationManager.AppSettings["GdaxPassphrase"] ?? "",
            };
        }
    }
}
