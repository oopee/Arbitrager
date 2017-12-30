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

        public GdaxSeller(GdaxConfiguration configuration, bool isSandbox)
        {
            m_authenticator = new GDAXClient.Authentication.Authenticator(configuration.Key, configuration.Signature, configuration.Passphrase);
            m_client = new GDAXClient.GDAXClient(m_authenticator, sandBox: isSandbox);
        }

        public async Task<IBidOrderBook> GetBids()
        {
            var result = await m_client.ProductsService.GetProductOrderBookAsync(GDAXClient.Services.Orders.ProductType.EthEur);

            var orderBook = new OrderBook();
            if (result.Asks.Any())
            {
                orderBook.Asks.AddRange(result.Asks.Select(x => new Order()
                {
                    PricePerUnit = x.First(),
                    VolumeUnits = x.Skip(1).First(),
                    Timestamp = DateTime.Now
                }));
            }

            if (result.Bids.Any())
            {
                orderBook.Bids.AddRange(result.Bids.Select(x => new Order()
                {
                    PricePerUnit = x.First(),
                    VolumeUnits = x.Skip(1).First(),
                    Timestamp = DateTime.Now
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
    }

    public class GdaxConfiguration
    {
        public string Key { get; set; }
        public string Signature { get; set; }
        public string Passphrase { get; set; }
    }
}
