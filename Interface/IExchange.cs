﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public interface IExchange
    {
        string Name { get; }
        decimal TakerFeePercentage { get; }
        decimal MakerFeePercentage { get; }

        Task<BalanceResult> GetCurrentBalance();

        Task<List<FullMyOrder>> GetOpenOrders();
        Task<List<FullMyOrder>> GetClosedOrders(GetOrderArgs args = null);
        Task<CancelOrderResult> CancelOrder(OrderId id);

        /// <summary>
        /// Withdraw fiat currency to bank account
        /// </summary>
        /// <param name="amount">Amount to withdraw</param>
        /// <param name="currency">Currency code</param>
        /// <param name="account">Account identifier</param>
        Task<WithdrawFiatResult> WithdrawFundsToBankAccount(decimal amount, string currency, string account);
        
        /// <summary>
        /// Withdraw cryptocurrency to given address
        /// </summary>
        /// <param name="amount">Amount to withdraw</param>
        /// <param name="currency">Currency code</param>
        /// <param name="address">Address</param>
        Task<WithdrawCryptoResult> WithdrawCryptoToAddress(decimal amount, string currency, string address);

        Task<PaymentMethodResult> GetPaymentMethods();
        Task<FullMyOrder> GetOrderInfo(OrderId id);
    }

    public class GetOrderArgs
    {
        public DateTime? StartUtc { get; set; }
        public DateTime? EndUtc { get; set; }
    }

    public class GenericResult
    {
        public bool WasCancelled { get; set; }
        public string Error { get; set; }
    }

    public class CancelOrderResult : GenericResult
    {
    }

    public class WithdrawFiatResult : GenericResult
    {
        public string ReferenceId { get; set; }
    }

    public class WithdrawCryptoResult : GenericResult
    {
        public string ReferenceId { get; set; }
    }

    public class PaymentMethodResult
    {
        public List<PaymentMethod> Methods { get; set; } = new List<PaymentMethod>();
    }
}
