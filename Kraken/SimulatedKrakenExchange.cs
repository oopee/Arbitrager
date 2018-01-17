using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Kraken
{
    public class FakeKrakenExchange : Common.Simulation.SimulatedExchangeBase
    {
        string m_orderBookJson;

        public static string DefaultOrderBookJson = "{'Asks':[{'PricePerUnit':848.25800,'VolumeUnits':2.853,'Timestamp':'2018-01-08T14:59:39Z'},{'PricePerUnit':848.69981,'VolumeUnits':4.310,'Timestamp':'2018-01-08T14:59:22Z'},{'PricePerUnit':849.00000,'VolumeUnits':0.100,'Timestamp':'2018-01-08T14:59:43Z'},{'PricePerUnit':849.35000,'VolumeUnits':4.100,'Timestamp':'2018-01-08T14:59:43Z'},{'PricePerUnit':849.55000,'VolumeUnits':4.600,'Timestamp':'2018-01-08T14:59:44Z'},{'PricePerUnit':850.00000,'VolumeUnits':330.178,'Timestamp':'2018-01-08T14:59:35Z'},{'PricePerUnit':850.91000,'VolumeUnits':3.000,'Timestamp':'2018-01-08T14:58:40Z'},{'PricePerUnit':851.00000,'VolumeUnits':20.211,'Timestamp':'2018-01-08T14:58:34Z'},{'PricePerUnit':851.30000,'VolumeUnits':1.580,'Timestamp':'2018-01-08T14:58:01Z'},{'PricePerUnit':851.31000,'VolumeUnits':1.500,'Timestamp':'2018-01-08T14:58:47Z'},{'PricePerUnit':851.40000,'VolumeUnits':1.590,'Timestamp':'2018-01-08T14:57:12Z'},{'PricePerUnit':851.41000,'VolumeUnits':4.320,'Timestamp':'2018-01-08T14:59:42Z'},{'PricePerUnit':851.50000,'VolumeUnits':30.000,'Timestamp':'2018-01-08T14:58:20Z'},{'PricePerUnit':852.00000,'VolumeUnits':1.659,'Timestamp':'2018-01-08T14:56:59Z'},{'PricePerUnit':855.00000,'VolumeUnits':0.554,'Timestamp':'2018-01-08T14:57:16Z'}],'Bids':[{'PricePerUnit':840.00000,'VolumeUnits':38.782,'Timestamp':'2018-01-08T14:59:04Z'},{'PricePerUnit':840.09000,'VolumeUnits':0.500,'Timestamp':'2018-01-07T10:06:14Z'},{'PricePerUnit':840.47000,'VolumeUnits':0.398,'Timestamp':'2018-01-08T13:15:39Z'},{'PricePerUnit':840.60000,'VolumeUnits':44.491,'Timestamp':'2018-01-08T14:58:57Z'},{'PricePerUnit':841.00000,'VolumeUnits':25.836,'Timestamp':'2018-01-08T14:52:28Z'},{'PricePerUnit':841.13000,'VolumeUnits':0.200,'Timestamp':'2018-01-07T11:54:45Z'},{'PricePerUnit':841.17000,'VolumeUnits':129.915,'Timestamp':'2018-01-08T14:57:51Z'},{'PricePerUnit':841.18000,'VolumeUnits':29.412,'Timestamp':'2018-01-08T14:59:22Z'},{'PricePerUnit':841.41000,'VolumeUnits':2.580,'Timestamp':'2018-01-08T14:59:47Z'},{'PricePerUnit':841.69000,'VolumeUnits':24.625,'Timestamp':'2018-01-08T14:59:35Z'},{'PricePerUnit':842.10000,'VolumeUnits':229.334,'Timestamp':'2018-01-08T14:59:31Z'},{'PricePerUnit':845.00000,'VolumeUnits':0.070,'Timestamp':'2018-01-08T14:59:37Z'},{'PricePerUnit':846.19000,'VolumeUnits':44.491,'Timestamp':'2018-01-08T14:59:36Z'},{'PricePerUnit':847.52000,'VolumeUnits':0.385,'Timestamp':'2018-01-08T14:59:26Z'}]}";

        public FakeKrakenExchange(ILogger logger, string orderBookJson = null)
            : base("FakeKrakenExchange")
        {
            TakerFeePercentage = PercentageValue.FromPercentage(0.3m);
            MakerFeePercentage = PercentageValue.FromPercentage(0.0m);
            m_orderBookJson = orderBookJson ?? DefaultOrderBookJson;
        }

        public override async Task<IOrderBook> GetOrderBook(AssetPair assetPair)
        {
            await Task.Yield();
            return Common.Simulation.SimulatedExchangeBase.SimpleOrderBook.GetOrderBookFromJson(m_orderBookJson, assetPair);
        }
    }

    public class SimulatedKrakenExchange : Common.Simulation.SimulatedExchangeBase
    {
        private KrakenExchange m_realKraken;

        public SimulatedKrakenExchange(KrakenConfiguration configuration, ILogger logger)
            : base("SimulatedKraken")
        {
            m_realKraken = new KrakenExchange(configuration, logger);

            TakerFeePercentage = m_realKraken.TakerFeePercentage;
            MakerFeePercentage = m_realKraken.MakerFeePercentage;
        }

        public override Task<IOrderBook> GetOrderBook(AssetPair assetPair)
        {
            return m_realKraken.GetOrderBook(assetPair);
        }
    } 
}
