using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using Interface;
using Common;
using System.Numerics;
using System.Globalization;
using Newtonsoft.Json;

namespace Tests
{
    [TestFixture]
    public class Runner : TestBase
    {
        public static AssetPair EthEur = new AssetPair(Asset.ETH, Asset.EUR);

        [Explicit]
        [Test]
        public async Task Binance_Misc()
        {
            /* TODO testitärpit:
             * - free vs locked balance varmistaa ero
            */

            var conf = Binance.BinanceConfiguration.FromAppConfig();

            // AssetPair assetPair = new AssetPair(Asset.ETH, Asset.BTC);
            // AssetPair assetPair = new AssetPair(Asset.BTC, Asset.ETH);
            AssetPair assetPair = new AssetPair(Asset.NEO, Asset.USDT);

            //var result = await GetBinance().GetCurrentBalance(assetPair);

            var result = await GetBinance().GetOrderBook(assetPair);
            
            await Task.Delay(1000);
            
            var result2 = await GetBinance().GetCurrentBalance(assetPair);
            
            await Task.Delay(5000);

            var result3 = await GetBinance().GetOrderBook(assetPair);

            // var book = await binanceClient.GetOrderBook("ethbtc");
            var serialized = JsonConvert.SerializeObject(result);
            Logger.Info(serialized);
        }

        [Explicit]
        [Test]
        public async Task Kraken_GetOpenOrders()
        {
            var openOrders = await GetKraken().GetOpenOrders();
            Logger.Info(GetDebugString(openOrders));
        }

        [Explicit]
        [Test]
        public async Task Kraken_Gdax_GetStatus()
        {
            var result = await GetKrakenGdaxArbitrager().GetStatus(false);
            Logger.Info(result.ToString());
        }

        [Explicit]
        [Test]
        public async Task Kraken_Gdax_GetStatus_WithBalance()
        {
            var result = await GetKrakenGdaxArbitrager().GetStatus(true);
            Logger.Info(result.ToString());
        }

        [Explicit]
        [Test]
        public async Task Kraken_Gdax_GetArbitrageInfo()
        {
            var info = await GetKrakenGdaxArbitrager().GetInfoForArbitrage(
                PriceValue.FromEUR(decimal.MaxValue), 
                BalanceOption.CapToBalance, 
                PriceValue.FromETH(decimal.MaxValue), 
                BalanceOption.IgnoreBalance);
            Logger.Info(info.ToString());
        }

        [Explicit]
        [Test]
        public async Task Kraken_Gdax_GetAccounts()
        {
            var result = await GetKrakenGdaxArbitrager().GetAccountsInfo();
            Logger.Info(result.ToString());
        }

        [Explicit]
        [Test]
        public async Task Kraken_ShowOrderInfo()
        {
            var exchange = GetKraken();

            var info = await exchange.GetOrderInfo(new OrderId("OEJ2TW-FIVDX-ITK2EW"));

            /*
            var order = await ((IBuyer)exchange).PlaceImmediateBuyOrder(1200, 0.02m);
            var info = await exchange.GetOrderInfo(order.Id);
            await exchange.CancelOrder(order.Id);
            Logger.Info(GetDebugString(info));
            */
        }

        [Explicit]
        [Test]
        public async Task Gdax_ShowOrderInfo()
        {
            /*
            var order = await ((ISeller)GetGdax()).GetOrderInfo(new OrderId(new Guid("160d8288-08ca-477c-b19f-c2753e2f5070").ToString()));
            var order2 = await ((ISeller)GetGdax()).GetOrderInfo(new OrderId(new Guid("d76171c3-abe7-43fd-bfe1-ec75396d9848").ToString()));
            */
            var order3 = await (GetGdax()).GetOrderInfo(new OrderId(new Guid("99e7a555-7dee-4997-acb1-878abf912f7c").ToString()));
            
            /*var exchange = GetGdax();

            var order = await ((ISeller)exchange).PlaceSellOrder(99999m, 0.001m);
            var info = await exchange.GetOrderInfo(order.Id);
            await exchange.CancelOrder(order.Id);
            Logger.Info(GetDebugString(info));*/
        }

        private void EURTests (bool positive)
        {
            int factor = positive ? 1 : -1;
            var p1 = PercentageValue.FromString("  57.5  %  ");
            var p2 = PercentageValue.FromRatio(0.26m);
            var p3 = PercentageValue.FromPercentage(30);

            // 100 cents = 1EUR
            var v1 = 235768.33m;
            var v2 = 21.82m;
            PriceValue euroValue1 = PriceValue.FromEUR(v1); // 235768,33
            PriceValue euroValue2 = PriceValue.FromEUR(v2) * factor; // 21,82
            
            PriceValue eur = euroValue1 / euroValue2;
            Assert.AreEqual(eur.Value, 10805.148029330889092575618698442m * factor);
            eur = eur.Round();
            Assert.AreEqual(eur.Value, 10805.14m * factor);
            eur = euroValue1 * euroValue2;
            Assert.AreEqual(eur.Value, 5144464.9606m * factor);
            eur = eur.Round();
            Assert.AreEqual(eur.Value, 5144464.96m * factor);

            var sum = v1 + v2;
            var difference = v1 - v2;
            eur = euroValue1 - euroValue2;
            Assert.AreEqual(eur.Value, positive ? difference : sum);
            eur = euroValue1 + euroValue2;
            Assert.AreEqual(eur.Value, positive ? sum : difference);

            // PercentageValue
            eur = (euroValue1 * factor) * p1;
            Assert.AreEqual(eur.Value, 135566.78975m * factor);
            eur = (euroValue1 * factor).AddPercentage(p2);
            Assert.AreEqual(eur.Value, 297068.0958m * factor);
            eur = (euroValue1 * factor).SubtractPercentage(p3);
            Assert.AreEqual(eur.Value, 165037.831m * factor);

            // ROUNDINGS
            var round = PriceValue.FromEUR(1.33333333333m).Round(RoundingStrategy.AlwaysRoundDown);
            Assert.AreEqual(round.Value, 1.33m);
            round = PriceValue.FromEUR(1.335m).Round(RoundingStrategy.Default);
            Assert.AreEqual(round.Value, 1.34m);
            round = PriceValue.FromEUR(1.33333333333m).Round(RoundingStrategy.AlwaysRoundUp);
            Assert.AreEqual(round.Value, 1.34m);
            round = PriceValue.FromEUR(1.33333333333m).Round(decimalPlaces: 6);
            Assert.AreEqual(round.Value, 1.333333m);

            // ToString
            var pv = PriceValue.FromEUR(1.333333m);
            Assert.AreEqual(pv.ToString(), "1,33");
            Assert.AreEqual(pv.ToStringWithAsset(), "1,33 EUR");
        }

        private void ETHTests(bool positive)
        {
            int factor = positive ? 1 : -1;
            var p1 = PercentageValue.FromPercentage(0.2663m);
            PercentageValue p2 = PercentageValue.FromRatio(0.112m);
            
            // 1000000000000000000 wei = 1 ETH
            var v1 = 11.158978324796219532m;
            var v2 = 50.155700000000000000m;
            var v3 = 50000000.9826384562m;
            PriceValue ethValue1 = PriceValue.FromETH(v1); // 11,158978324796219532
            PriceValue ethValue2 = PriceValue.FromETH(v2) * factor; // 50,155700000000000000
            PriceValue ethValue3 = PriceValue.FromETH(v3); // 50000000,9826384562

            PriceValue eth = ethValue1 / ethValue2;
            Assert.AreEqual(eth.Value, 0.22248674277891086221506229601022m * factor);
            eth = eth.Round();
            Assert.AreEqual(eth.Value, 0.222486742778910862m * factor);
            eth = ethValue2 * ethValue3;
            Assert.AreEqual(eth.Value, 2507785049.28491961763034m * factor);
            eth = eth.Round();
            Assert.AreEqual(eth.Value, 2507785049.28491961763034m * factor);
            
            eth = ethValue2 - ethValue1;
            Assert.AreEqual(eth.Value, positive ? v2 - v1 : (v2 * -1) - v1);
            eth = ethValue2 + ethValue1;
            Assert.AreEqual(eth.Value, positive ? v2 + v1 : (v2 * -1) + v1);

            // PercentageValue
            eth = (ethValue1 * factor) * p1;
            Assert.AreEqual(eth.Value, 0.029716359278932332613716 * factor);
            eth = (ethValue1 * factor).AddPercentage(p2);
            Assert.AreEqual(eth.Value, 12.408783897173396119584m * factor);
            eth = (ethValue1 * factor).SubtractPercentage(p2);
            Assert.AreEqual(eth.Value, 9.909172752419042944416m * factor);

            // ROUNDINGS
            var round = PriceValue.FromETH(2.9716359278932332613716m).Round(RoundingStrategy.AlwaysRoundDown);
            Assert.AreEqual(round.Value, 2.971635927893233261m);
            round = PriceValue.FromETH(12.408783897173396119584m).Round(RoundingStrategy.Default);
            Assert.AreEqual(round.Value, 12.408783897173396120m);
            round = PriceValue.FromETH(2.9716359278932332613716m).Round(RoundingStrategy.AlwaysRoundUp);
            Assert.AreEqual(round.Value, 2.971635927893233262m);
            round = PriceValue.FromETH(2.9716359278932332613716m).Round(RoundingStrategy.Default, decimalPlaces: 10);
            Assert.AreEqual(round.Value, 2.9716359279m);

            // ToString
            var pv = PriceValue.FromETH(2.97m);
            Assert.AreEqual(pv.ToString(), "2,970000000000000000");
            Assert.AreEqual(pv.ToStringWithAsset(), "2,970000000000000000 ETH");
        }

        [Explicit]
        [Test]
        public async Task Gdax_PlaceSellorder()
        {
            var exchange = GetGdax();

            var order = await exchange.PlaceImmediateSellOrder(EthEur, PriceValue.FromEUR(99999m), PriceValue.FromETH(0.001m));
            var info = await exchange.GetOrderInfo(order.Id);
            Logger.Info(GetDebugString(info));
        }

        [Explicit]
        [Test]
        public async Task Kraken_PlaceBuyOrder()
        {
            var exchange = GetKraken();

            var order = await exchange.PlaceImmediateBuyOrder(EthEur, PriceValue.FromEUR(0.01m), PriceValue.FromETH(0.2m));
            var info = await exchange.GetOrderInfo(order.Id);
            Logger.Info(GetDebugString(info));
        }

        [Explicit]
        [Test]
        public async Task Kraken_PlaceSellOrder()
        {
            var exchange = GetKraken();

            var order = await exchange.PlaceImmediateSellOrder(EthEur, PriceValue.FromEUR(99999m), PriceValue.FromETH(0.2m));
            var info = await exchange.GetOrderInfo(order.Id);
            Logger.Info(GetDebugString(info));
        }

        [Explicit]
        [Test]
        public async Task PriceValue_Test()
        {
            EURTests(true);
            EURTests(false);

            ETHTests(true);
            ETHTests(false);
            
            await Task.Delay(0);
        }        
    }
}
