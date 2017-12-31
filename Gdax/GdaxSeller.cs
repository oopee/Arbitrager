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
                Ids = new List<OrderId>() { new OrderId(order.Id.ToString()) },
                PricePerUnit = order.Price,
                Volume = order.Size,
                Type = OrderType.Sell
            };

            Logger.Info("GdaxSeller: placed sell order {0}", orderResult);

            return orderResult;
        }

        public async Task<List<FullMyOrder>> GetOpenOrders()
        {
            var orders = await m_client.OrdersService.GetAllOrdersAsync();

            // TODO

            return new List<FullMyOrder>();
        }

        public Task<List<FullMyOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            throw new NotImplementedException();
        }
    }

    public class GdaxConfiguration
    {
        public string Key { get; set; }
        public string Signature { get; set; }
        public string Passphrase { get; set; }
    }
}
