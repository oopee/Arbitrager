using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Interface;

namespace ArbitrageDataOutputter
{
    interface IArbitrageDataOutputter
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
            }
            catch (Exception e)
            {
                Console.WriteLine("Error outputting data:");
                Console.WriteLine(e.ToString());
            }
        }

        public virtual async Task Start()
        {
            Timer?.Stop();

            await DoOutput();

            Timer = new Timer();
            Timer.Elapsed += new ElapsedEventHandler(TimerEvent);
            Timer.Interval = (double)Interval * 1000.0;
            Timer.Enabled = true;

            Timer.Start();

            OnStarted();
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
