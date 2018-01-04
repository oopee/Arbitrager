﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface ISeller : IExchange
    {
        Task<IBidOrderBook> GetBids();
        Task<MyOrder> PlaceMarketSellOrder(decimal volume);
    }
}
