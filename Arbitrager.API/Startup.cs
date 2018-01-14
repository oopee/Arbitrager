using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Gdax;
using Interface;
using Kraken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arbitrager.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            var logger = new ConsoleLogger();
            Logger.StaticLogger = logger;
            services.AddSingleton<Interface.ILogger>(logger);

            var configuration = Configuration.GetValue<string>("Configuration");
            Console.WriteLine("Using configuration: {0}", configuration);
            IBuyer buyer;
            ISeller seller;

            switch (configuration)
            {
                case "fake":
                    buyer = new FakeBuyer(Logger.StaticLogger) { BalanceEth = PriceValue.FromETH(0m), BalanceEur = PriceValue.FromEUR(2000m) };
                    seller = new FakeSeller(Logger.StaticLogger) { BalanceEth = PriceValue.FromETH(1m), BalanceEur = PriceValue.FromEUR(0m) };
                    break;
                case "simulated":
                    buyer = new SimulatedKrakenBuyer(KrakenConfiguration.FromAppConfig(), Logger.StaticLogger) { BalanceEth = PriceValue.FromETH(0m), BalanceEur = PriceValue.FromEUR(2000m) };
                    seller = new SimulatedGdaxSeller(GdaxConfiguration.FromAppConfig(), Logger.StaticLogger, isSandbox: false) { BalanceEth = PriceValue.FromETH(1m), BalanceEur = PriceValue.FromEUR(0m) };
                    break;
                case "real":
                    buyer = new KrakenBuyer(GetKrakenConfiguration(), Logger.StaticLogger);
                    seller = new GdaxSeller(GetGdaxConfiguration(), Logger.StaticLogger, isSandbox: false);
                    break;
                default:
                    throw new ArgumentException("Invalid configuration (see appsettings.json). Valid values are: fake, simulated, real");
            }

            services.AddSingleton(buyer);
            services.AddSingleton(seller);
            services.AddSingleton<IProfitCalculator, DefaultProfitCalculator>();
            services.AddSingleton<IDatabaseAccess>(new DatabaseAccess.DatabaseAccess(configuration));
            services.AddSingleton<IArbitrager, DefaultArbitrager>();
        }

        private KrakenConfiguration GetKrakenConfiguration()
        {
            var conf = new KrakenConfiguration();
            Configuration.GetSection("Kraken").Bind(conf);
            return conf;
        }

        private GdaxConfiguration GetGdaxConfiguration()
        {
            var conf = new GdaxConfiguration();
            Configuration.GetSection("Gdax").Bind(conf);
            return conf;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true,
                    ReactHotModuleReplacement = true
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
