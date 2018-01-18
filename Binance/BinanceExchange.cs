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

        public async Task<BalanceResult> GetCurrentBalance(AssetPair assetPair)
        {
            var accountInfo = await m_client.GetAccountInfo(10000);

            // TODO mikko onko ok ottaa vain free, miten locked?
            var all = accountInfo.Balances.ToDictionary(x => x.Asset, x => x.Free);

            var binanceQuote = assetPair.Quote.ToString();
            var binanceBase = assetPair.Base.ToString();

            return new BalanceResult()
            {
                AssetPair = assetPair,
                All = all,
                QuoteCurrency = new PriceValue(all.Where(x => x.Key == binanceQuote).FirstOrDefault().Value, assetPair.Quote),
                BaseCurrency = new PriceValue(all.Where(x => x.Key == binanceBase).FirstOrDefault().Value, assetPair.Base),
            };
        }

        public async Task<IOrderBook> GetOrderBook(AssetPair assetPair)
        {
            var result = await m_client.GetOrderBook(assetPair.ShortName);

            var orderBook = new OrderBook()
            {
                AssetPair = assetPair
            };

            if (result.Asks.Any())
            {
                orderBook.Asks.AddRange(result.Asks.Select(x => new OrderBookOrder()
                {
                    PricePerUnit = new PriceValue(x.Price, assetPair.Quote),
                    VolumeUnits = new PriceValue(x.Quantity, assetPair.Base),
                    Timestamp = TimeService.UtcNow
                }));
            }

            if (result.Bids.Any())
            {
                orderBook.Bids.AddRange(result.Bids.Select(x => new OrderBookOrder()
                {
                    PricePerUnit = new PriceValue(x.Price, assetPair.Quote),
                    VolumeUnits = new PriceValue(x.Quantity, assetPair.Base),
                    Timestamp = TimeService.UtcNow
                }));
            }

            return orderBook;
        }
        
        public Task<CancelOrderResult> CancelOrder(OrderId id)
        {
            throw new NotImplementedException();
        }

        public Task<List<FullOrder>> GetClosedOrders(GetOrderArgs args = null)
        {
            throw new NotImplementedException();
        }
        
        public Task<List<FullOrder>> GetOpenOrders()
        {
            throw new NotImplementedException();
        }

        public Task<FullOrder> GetOrderInfo(OrderId id)
        {
            throw new NotImplementedException();
        }

        public Task<PaymentMethodResult> GetPaymentMethods()
        {
            return Task.FromResult(new PaymentMethodResult());
        }

        public Task<MinimalOrder> PlaceImmediateBuyOrder(AssetPair assetPair, PriceValue limitPricePerUnit, PriceValue maxVolume)
        {
            throw new NotImplementedException();
        }

        public Task<MinimalOrder> PlaceImmediateSellOrder(AssetPair assetPair, PriceValue minLimitPrice, PriceValue volume)
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
