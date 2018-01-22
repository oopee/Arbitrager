using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Interface;

namespace Common
{
    public class ProductCache
    {
        Dictionary<AssetPair, Product> m_products = new Dictionary<AssetPair, Product>();
        Func<Task<ProductResult>> m_refresher;

        Task m_refreshTask;

        public ProductCache(Func<Task<ProductResult>> refresher)
        {
            m_refresher = refresher;
        }

        public async Task<Product> Get(AssetPair assetPair)
        {
            await RefreshIfRequired();

            m_products.TryGetValue(assetPair, out Product value);
            return value;
        }

        public async Task<ProductResult> GetAll()
        {
            await RefreshIfRequired();

            return new ProductResult()
            {
                Products = new List<Product>(m_products.Values)
            };
        }

        private async Task RefreshIfRequired()
        {
            // For now do refreshing only ONCE 
            // (if multiple threads access this method at the same time, only first thread is doing the actual work and the 
            // others wait for the work to complete before returning)
            lock (this)
            {
                if (m_refreshTask == null)
                {
                    m_refreshTask = DoRefresh();
                }
            }

            await m_refreshTask;
        }

        private async Task DoRefresh()
        {
            var result = await m_refresher();
            var dict = result.Products.ToDictionary(x => x.AssetPair, x => x);
            System.Threading.Interlocked.Exchange(ref m_products, dict);
        }
    }
}
