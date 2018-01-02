﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Interface;

namespace Common
{
    public class DefaultArbitrager : IArbitrager
    {
        IBuyer m_buyer;
        ISeller m_seller;
        ILogger m_logger;

        public decimal ChunkEur { get; set; } = 2000m;

        public IBuyer Buyer => m_buyer;
        public ISeller Seller => m_seller;

        public DefaultArbitrager(IBuyer buyer, ISeller seller, ILogger logger)
        {
            m_buyer = buyer;
            m_seller = seller;
            m_logger = logger;
        }

        public async Task<AccountsInfo> GetAccountsInfo()
        {
            var buyer = await GetAccountInfo(m_buyer);
            var seller = await GetAccountInfo(m_seller);

            return new AccountsInfo()
            {
                Accounts = new List<AccountInfo>() { buyer, seller }
            };
        }

        private async Task<AccountInfo> GetAccountInfo(IExchange exchange)
        {
            var balance = await exchange.GetCurrentBalance();
            var methods = await exchange.GetPaymentMethods();

            return new AccountInfo()
            {
                Name = exchange.Name,
                Balance = balance,
                PaymentMethods = methods.Methods
            };
        }

        public async Task<Status> GetStatus(bool includeBalance)
        {
            BalanceResult buyerBalance = null;
            IAskOrderBook askOrderBook = null;

            BalanceResult sellerBalance = null;
            IBidOrderBook bidOrderBook = null;

            Func<Task> buyerTaskFunc = async () =>
            {
                if (includeBalance)
                {
                    buyerBalance = await m_buyer.GetCurrentBalance();
                }

                askOrderBook = await m_buyer.GetAsks();
            };

            Func<Task> sellerTaskFunc = async () =>
            {
                if (includeBalance)
                {
                    sellerBalance = await m_seller.GetCurrentBalance();
                }

                bidOrderBook = await m_seller.GetBids();
            };

            var buyerTask = buyerTaskFunc();
            var sellerTask = sellerTaskFunc();

            await buyerTask;
            await sellerTask;

            BuyerStatus buyerStatus = null;
            if (askOrderBook != null)
            {
                buyerStatus = new BuyerStatus()
                {
                    Name = m_buyer.Name,
                    Asks = askOrderBook,
                    Balance = buyerBalance
                };
            }

            SellerStatus sellerStatus = null;
            if (bidOrderBook != null)
            {
                sellerStatus = new SellerStatus()
                {
                    Name = m_seller.Name,
                    Bids = bidOrderBook,
                    Balance = sellerBalance
                };
            }

            return new Status(buyerStatus, sellerStatus);
        }
    }
}
