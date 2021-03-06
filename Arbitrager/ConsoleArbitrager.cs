﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Interface;
using Kraken;
using Gdax;
using System.Configuration;
using Common;

namespace Arbitrager
{
    public class ConsoleArbitrager : Common.DefaultArbitrager
    {
        public ConsoleArbitrager(AssetPair assetPair, IExchange buyer, IExchange seller, IProfitCalculator profitCalculator, IDatabaseAccess dataAccess, ILogger logger)
            : base(assetPair, new[] { buyer, seller }, profitCalculator, dataAccess, logger)
        {
        }

        protected override async Task DoArbitrage_CheckStatus(ArbitrageContext ctx)
        {
            await base.DoArbitrage_CheckStatus(ctx);

            if (ctx.Error != null)
            {
                return;
            }

            Console.WriteLine(ctx.Info);
            if (!ConsoleUtils.Confirm())
            {
                ctx.Error = ArbitrageError.ManuallyAborted;
            }
        }
    }

}
