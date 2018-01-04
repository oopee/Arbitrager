using GDAXClient.HttpClient;
using GDAXClient.Services;
using GDAXClient.Services.Accounts;
using GDAXClient.Services.HttpRequest;
using GDAXClient.Services.Orders;
using GDAXClient.Services.Products;
using GDAXClient.Services.Products.Models;
using GDAXClient.Services.Products.Models.Responses;
using GDAXClient.Utilities.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GDAXClient.Products
{
    public class ProductsService : AbstractService
    {
        private readonly IHttpRequestMessageService httpRequestMessageService;

        private readonly IHttpClient httpClient;

        private readonly IAuthenticator authenticator;

        public enum OrderBookLevel
        {
            /// <summary>
            /// Only the best bid and ask
            /// </summary>
            Top1 = 1,

            /// <summary>
            /// Top 50 bids and asks (aggregated)
            /// </summary>
            Top50 = 2,

            /* Full not yet supported, because it returns data in a different format,
             * see: https://docs.gdax.com/#get-product-order-book

            /// <summary>
            /// Full order book (non aggregated)
            /// </summary>
            Full = 3

            */
        }

        public ProductsService(
            IHttpClient httpClient,
            IHttpRequestMessageService httpRequestMessageService,
            IAuthenticator authenticator)
                : base(httpClient, httpRequestMessageService, authenticator)
        {
            this.httpRequestMessageService = httpRequestMessageService;
            this.httpClient = httpClient;
            this.authenticator = authenticator;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            var httpResponseMessage = await SendHttpRequestMessageAsync(HttpMethod.Get, authenticator, "/products");
            var contentBody = await httpClient.ReadAsStringAsync(httpResponseMessage).ConfigureAwait(false);
            var productsResponse = JsonConvert.DeserializeObject<IEnumerable<Product>>(contentBody);

            return productsResponse;
        }

        public async Task<ProductsOrderBookResponse> GetProductOrderBookAsync(ProductType productPair, OrderBookLevel level = OrderBookLevel.Top1)
        {
            string uri = $"/products/{productPair.ToDasherizedUpper()}/book?level={((int)level).ToString()}";

            var httpResponseMessage = await SendHttpRequestMessageAsync(HttpMethod.Get, authenticator, uri);
            var contentBody = await httpClient.ReadAsStringAsync(httpResponseMessage).ConfigureAwait(false);
            var productOrderBookResponse = JsonConvert.DeserializeObject<ProductsOrderBookResponse>(contentBody);

            return productOrderBookResponse;
        }

        public async Task<ProductTicker> GetProductTickerAsync(ProductType productPair)
        {
            var httpResponseMessage = await SendHttpRequestMessageAsync(HttpMethod.Get, authenticator, $"/products/{productPair.ToDasherizedUpper()}/ticker");
            var contentBody = await httpClient.ReadAsStringAsync(httpResponseMessage).ConfigureAwait(false);
            var productTickerResponse = JsonConvert.DeserializeObject<ProductTicker>(contentBody);

            return productTickerResponse;
        }

        public async Task<ProductStats> GetProductStatsAsync(ProductType productPair)
        {
            var httpResponseMessage = await SendHttpRequestMessageAsync(HttpMethod.Get, authenticator, $"/products/{productPair.ToDasherizedUpper()}/stats");
            var contentBody = await httpClient.ReadAsStringAsync(httpResponseMessage).ConfigureAwait(false);
            var productStatsResponse = JsonConvert.DeserializeObject<ProductStats>(contentBody);

            return productStatsResponse;
        }
    }
}
