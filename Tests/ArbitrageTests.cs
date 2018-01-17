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
    public class ArbitrageTests : TestBase
    {
        [Test]
        public async Task Arbitrage_TestFull()
        {
            var arbitrager = GetTestArbitrager(2000, 3);
            var ctx = await arbitrager.Arbitrage(ArbitrageContext.Start(AssetPair.EthEur, 1000m));

            // TODO: Assert values after rounding has been implemented
            var buy = ctx.BuyOrder;
            var sell = ctx.SellOrder;

            Assert.AreEqual(999.9999999999999999999999999m, buy.CostIncludingFee.Value);
        }

        TestArbitrager GetTestArbitrager(decimal eurBalance, decimal ethBalance)
        {
            var arbitrager = new TestArbitrager(DataAccess, Logger);
            arbitrager.Buyer.BalanceQuote = PriceValue.FromEUR(eurBalance);
            arbitrager.Seller.BalanceBase = PriceValue.FromETH(ethBalance);
            return arbitrager;
        }
    }

    public class TestArbitrager : Common.DefaultArbitrager
    {
        public Kraken.FakeKrakenExchange Buyer => (Kraken.FakeKrakenExchange)Exchanges[0];
        public Gdax.FakeGdaxExchange Seller => (Gdax.FakeGdaxExchange)Exchanges[1];

        public TestArbitrager(IDatabaseAccess dataAccess, ILogger logger) 
            : base(
                  AssetPair.EthEur,
                  new IExchange[]
                  {
                    new Kraken.FakeKrakenExchange(logger),
                    new Gdax.FakeGdaxExchange(logger)
                  },
                  new DefaultProfitCalculator(),
                  dataAccess, 
                  logger)
        {
        }

        protected override Task OnStateBegin(ArbitrageContext ctx)
        {
            return base.OnStateBegin(ctx);
        }

        protected override Task OnStateEnd(ArbitrageContext ctx)
        {
            return base.OnStateEnd(ctx);
        }
    }
}
