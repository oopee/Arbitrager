using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Interface;

namespace ArbitrageDataOutputter
{
    public interface IArbitrageDataOutputter
    {
        /// <summary>
        /// Output interval in seconds. Non-positive value means output is not only created upon manual calls to DoOutput().
        /// </summary>
        decimal Interval { get; set; }

        Task Initialize();

        Task DoOutput();

        Task Start();
        void Stop();
    }

    public abstract class AbstractArbitrageDataOutputter : IArbitrageDataOutputter
    {
        public IArbitrageDataSource DataSource { get; private set; }
        public decimal Interval { get; set; }

        private Timer Timer { get; set; }

        private System.Net.HttpStatusCode? LastErrorCode { get; set; }
        private DateTime? LastErrorTime { get; set; }
        private DateTime? LastErrorPrintTime { get; set; }
        private int ErrorCount { get; set; }

        public AbstractArbitrageDataOutputter(IArbitrageDataSource dataSource)
        {
            DataSource = dataSource;
        }

        public abstract Task Initialize();

        public virtual async Task DoOutput()
        {
            try
            {
                var data = await DataSource.GetCurrentData();
                await OutputData(data);

                if (LastErrorTime != null)
                {
                    Console.WriteLine($"{DateTime.Now} Got successful output after {ErrorCount} errors");
                    LastErrorTime = null;
                    LastErrorPrintTime = null;
                    LastErrorCode = null;
                    ErrorCount = 0;
                }
            }
            catch (System.Net.WebException e)
            {
                var response = (System.Net.HttpWebResponse)e.Response;
                if (LastErrorPrintTime == null)
                {
                    Console.WriteLine($"{DateTime.Now} Got HTTP error response {(int)response.StatusCode} {response.StatusCode}");
                    LastErrorPrintTime = DateTime.Now;
                }
                else if ((DateTime.Now - LastErrorPrintTime.Value).TotalMinutes > 15)
                {
                    Console.WriteLine($"{DateTime.Now} API still returning HTTP error response {(int)response.StatusCode} {response.StatusCode}");
                    LastErrorPrintTime = DateTime.Now;
                }

                LastErrorTime = DateTime.Now;
                LastErrorCode = response.StatusCode;
                ++ErrorCount;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error outputting data:");
                Console.WriteLine(e.ToString());

                LastErrorTime = DateTime.Now;
                ++ErrorCount;
            }
        }

        public virtual async Task Start()
        {
            Timer?.Stop();

            OnStarted();

            await DoOutput();

            Timer = new Timer();
            Timer.Elapsed += new ElapsedEventHandler(TimerEvent);
            Timer.Interval = (double)Interval * 1000.0;
            Timer.Enabled = true;

            Timer.Start();
        }

        private async void TimerEvent(object sender, ElapsedEventArgs e)
        {
            await DoOutput();
        }

        public virtual void Stop()
        {
            Timer?.Stop();
            Timer = null;

            OnStopped();
        }

        protected virtual void OnStarted()
        {
        }

        protected virtual void OnStopped()
        {
        }

        protected abstract Task OutputData(ArbitrageDataPoint info);
    }
}
