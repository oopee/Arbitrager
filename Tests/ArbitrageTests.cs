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
            var ctx = await arbitrager.Arbitrage(ArbitrageContext.Start(1000));

            // TODO: Assert values after rounding has been implemented
            var buy = ctx.BuyOrder;
            var sell = ctx.SellOrder;

            Assert.AreEqual(999.9999999999999999999999999m, buy.Cost);
        }

        TestArbitrager GetTestArbitrager(decimal eurBalance, decimal ethBalance)
        {
            var arbitrager = new TestArbitrager(DataAccess, Logger);
            arbitrager.Buyer.BalanceEur = eurBalance;
            arbitrager.Seller.BalanceEth = ethBalance;
            return arbitrager;
        }
    }

    public class TestArbitrager : Common.DefaultArbitrager
    {
        public new Kraken.FakeBuyer Buyer => (Kraken.FakeBuyer)base.Buyer;
        public new Gdax.FakeSeller Seller => (Gdax.FakeSeller)base.Seller;

        public TestArbitrager(IDatabaseAccess dataAccess, ILogger logger) 
            : base(
                  new Kraken.FakeBuyer(logger),
                  new Gdax.FakeSeller(logger),
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
