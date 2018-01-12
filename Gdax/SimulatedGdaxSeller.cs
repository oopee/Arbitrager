using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

using GDAXClient;

namespace Gdax
{
    public class FakeSeller : SimulatedGdaxSellerBase
    {
        OrderBook m_orderBook;

        public static string DefaultOrderBookJson = "{'Asks':[{'PricePerUnit':874.59,'VolumeUnits':15.60004117,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':874.6,'VolumeUnits':7.37404523,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':875.0,'VolumeUnits':10.72726774,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':876.0,'VolumeUnits':1.28,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':876.99,'VolumeUnits':2.92,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':877.0,'VolumeUnits':1.61346,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':878.0,'VolumeUnits':2.276,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':878.12,'VolumeUnits':7.4,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':878.47,'VolumeUnits':0.07585168,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':878.76,'VolumeUnits':4.35788,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':879.48,'VolumeUnits':0.8,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':879.83,'VolumeUnits':0.11433798,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':879.93,'VolumeUnits':10.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':879.98,'VolumeUnits':0.37349412,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':879.99,'VolumeUnits':5.08916056,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':880.0,'VolumeUnits':17.29086275,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':880.22,'VolumeUnits':1.35195824,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':881.0,'VolumeUnits':12.895,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':883.21,'VolumeUnits':1.27975502,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':883.22,'VolumeUnits':13.1438,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':883.39,'VolumeUnits':2.77798158,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':883.72,'VolumeUnits':0.07585168,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':883.99,'VolumeUnits':0.07303072,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':884.11,'VolumeUnits':0.02827702,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':885.0,'VolumeUnits':1.765,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':885.3,'VolumeUnits':7.01010097,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':886.28,'VolumeUnits':1.0018037,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':887.46,'VolumeUnits':0.02817028,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':887.97,'VolumeUnits':0.2,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':888.0,'VolumeUnits':0.92299823,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':888.96,'VolumeUnits':0.07585168,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':888.98,'VolumeUnits':0.29070008,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':888.99,'VolumeUnits':0.07303156,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':889.0,'VolumeUnits':0.02,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':889.98,'VolumeUnits':0.001,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':889.99,'VolumeUnits':0.1527232,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':890.0,'VolumeUnits':3.02808989,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':890.01,'VolumeUnits':0.03,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':890.04,'VolumeUnits':1.17445985,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':890.22,'VolumeUnits':131.43799,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':891.0,'VolumeUnits':8.49746444,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':892.21,'VolumeUnits':0.0280203,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':893.99,'VolumeUnits':1.27975502,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':894.0,'VolumeUnits':0.0086689,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':894.21,'VolumeUnits':0.0758517,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':894.44,'VolumeUnits':0.04,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':894.88,'VolumeUnits':0.0279367,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':894.99,'VolumeUnits':9.05436271,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':895.0,'VolumeUnits':0.00883799,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':895.4,'VolumeUnits':5.5852133,'Timestamp':'2018-01-08T14:59:38.1101898Z'}],'Bids':[{'PricePerUnit':874.0,'VolumeUnits':8.16541121,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':873.97,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':873.96,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':873.59,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':873.53,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':873.41,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':873.19,'VolumeUnits':5.51998992,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':873.09,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':873.04,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':873.0,'VolumeUnits':2.17707091,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':872.73,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':872.57,'VolumeUnits':0.0114604,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':872.52,'VolumeUnits':0.72744121,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':872.38,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':872.31,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':872.26,'VolumeUnits':0.02866117,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':872.2,'VolumeUnits':0.4,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':872.06,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':872.03,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':872.0,'VolumeUnits':0.52600413,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.87,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.78,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.71,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.7,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.69,'VolumeUnits':0.1,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.56,'VolumeUnits':0.80114789,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.31,'VolumeUnits':0.06,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.3,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.1,'VolumeUnits':0.05,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.01,'VolumeUnits':0.575,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':871.0,'VolumeUnits':6.80492314,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.78,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.75,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.74,'VolumeUnits':0.11484484,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.43,'VolumeUnits':0.08619964,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.4,'VolumeUnits':18.8706362,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.39,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.13,'VolumeUnits':8.24610981,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.11,'VolumeUnits':0.63,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.1,'VolumeUnits':2.5,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.05,'VolumeUnits':0.02187,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.01,'VolumeUnits':3.37844481,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':870.0,'VolumeUnits':36.2733488,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':869.99,'VolumeUnits':0.03,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':869.95,'VolumeUnits':0.56325076,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':869.94,'VolumeUnits':2.19561789,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':869.86,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':869.81,'VolumeUnits':0.171,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':869.78,'VolumeUnits':1.0,'Timestamp':'2018-01-08T14:59:38.1101898Z'},{'PricePerUnit':869.51,'VolumeUnits':0.05,'Timestamp':'2018-01-08T14:59:38.1101898Z'}]}";

        public FakeSeller(ILogger logger, string orderBookJson = null)
            : base("FakeSeller", logger)
        {
            TakerFeePercentage = 0.3m / 100;
            MakerFeePercentage = 0m / 100;

            m_orderBook = Newtonsoft.Json.JsonConvert.DeserializeObject<OrderBook>((orderBookJson ?? DefaultOrderBookJson).Replace("'", "\""));
        }

        public override async Task<IBidOrderBook> GetBids()
        {
            await Task.Yield();
            return m_orderBook;
        }
    }

    public class SimulatedGdaxSeller : SimulatedGdaxSellerBase
    {
        private GdaxSeller m_realSeller;

        public SimulatedGdaxSeller(GdaxConfiguration configuration, ILogger logger, bool isSandbox)
            : base("SimulatedGDAX", logger)
        {
            m_realSeller = new GdaxSeller(configuration, logger, isSandbox);

            TakerFeePercentage = m_realSeller.TakerFeePercentage;
            MakerFeePercentage = m_realSeller.MakerFeePercentage;
        }

        public override Task<IBidOrderBook> GetBids()
        {
            return m_realSeller.GetBids();
        }
    }

    public abstract class SimulatedGdaxSellerBase : Common.Simulation.SimulatedExchangeBase, Interface.ISeller
    {        
        protected ILogger m_logger;

        public SimulatedGdaxSellerBase(string name, ILogger logger)
            : base(name)
        {
            m_logger = logger.WithName(GetType().Name);
        }

        public abstract Task<IBidOrderBook> GetBids();

        public async Task<MinimalOrder> PlaceImmediateSellOrder(decimal minLimitPrice, decimal volume)
        {
            if (volume > BalanceEth)
            {
                throw new Exception("Invalid ETH balance");
            }

            var bids = await GetBids();

            // Limit order with Immediate or Cancel -> only take orders with unit price more than 'price' argument
            var orders = Common.DefaultProfitCalculator.GetFromOrderBook(bids.Bids.Where(x => x.PricePerUnit >= minLimitPrice), null, volume).ToList();

            var sum = orders.Select(x => x.PricePerUnit * x.VolumeUnits).DefaultIfEmpty().Sum();
            var fee = sum * TakerFeePercentage;
            var filledVolume = orders.Select(x => x.VolumeUnits).DefaultIfEmpty().Sum();
            var pricePerUnitWithoutFee = filledVolume == 0 ? 0 : sum / filledVolume;
            var pricePerUnitWithFee = filledVolume == 0 ? 0 : (sum - fee) / filledVolume;
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
    }    
}
