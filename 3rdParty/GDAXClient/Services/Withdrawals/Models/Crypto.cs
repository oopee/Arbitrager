using System;

namespace GDAXClient.Services.Withdrawals
{
    public class Crypto
    {
        public decimal amount { get; set; }

        public string currency { get; set; }

        public string crypto_address { get; set; }
    }
}
