using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using Interface;
using Common;

namespace Tests
{
    [TestFixture]
    public class Runner : TestBase
    {
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
            var info = await GetKrakenGdaxArbitrager().GetInfoForArbitrage(decimal.MaxValue, BalanceOption.CapToBalance, decimal.MaxValue, BalanceOption.IgnoreBalance);
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

            var order = await ((IBuyer)exchange).PlaceImmediateBuyOrder(0.1m, 1m);
            var info = await exchange.GetOrderInfo(order.Id);
            await exchange.CancelOrder(order.Id);
            Logger.Info(GetDebugString(info));
        }

        [Explicit]
        [Test]
        public async Task Gdax_ShowOrderInfo()
        {
            var order = await ((ISeller)GetGdax()).GetOrderInfo(new OrderId(new Guid("160d8288-08ca-477c-b19f-c2753e2f5070").ToString()));
            /*var exchange = GetGdax();

            var order = await ((ISeller)exchange).PlaceSellOrder(99999m, 0.001m);
            var info = await exchange.GetOrderInfo(order.Id);
            await exchange.CancelOrder(order.Id);
            Logger.Info(GetDebugString(info));*/
        }

        [Explicit]
        [Test]
        public async Task PriceValue_Test()
        {
            var eurPrice = PriceValue.FromEUR(123.9583734m);
            var eurPriceNegative = eurPrice * -1;
            Assert.AreEqual(eurPrice.Round().Value, 123.95m);
            Assert.AreEqual(eurPriceNegative.Round().Value, -123.95m);
            Assert.AreEqual(eurPrice.Round(RoundingStrategy.Default, 4).Value, 123.9584m);
            Assert.AreEqual(eurPrice.Round(RoundingStrategy.AlwaysRoundUp, 6).Value, 123.958374m);

            var ethPrice = PriceValue.FromETH(2.1999835m);
            Assert.AreEqual(ethPrice.Round().Value, 2.1999m);
            Assert.AreEqual(ethPrice.Round(RoundingStrategy.Default, 6).Value, 2.199984m);

            Assert.AreEqual((PriceValue.FromEUR(100m) + PriceValue.FromEUR(50.5m)).Value, 150.5m);
            await Task.Delay(0);
        }        
    }
}
