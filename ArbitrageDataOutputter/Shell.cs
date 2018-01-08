using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ArbitrageDataOutputter
{
    public class Shell
    {
        IArbitrageDataOutputter m_outputter = null;

        public Shell(IArbitrageDataOutputter outputter)
        {
            m_outputter = outputter;
        }

        public async Task Run()
        {
            await m_outputter.Initialize();
            await m_outputter.Start();

            Console.WriteLine($"Outputting data with {m_outputter.Interval} seconds interval, press 'q' to quit");

            while (Console.ReadKey(true).KeyChar != 'q')
            {
            }

            m_outputter.Stop();
        }
    }
}
