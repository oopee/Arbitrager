using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface;

namespace Common
{
    public abstract class LoggerBase : Interface.ILogger
    {
        public string Name { get; private set; }

        public void Error(string format, params object[] args)
        {
            Log(LogType.Error, format, args);
        }

        public void Info(string format, params object[] args)
        {
            Log(LogType.Info, format, args);
        }

        public void Debug(string format, params object[] args)
        {
            Log(LogType.Debug, format, args);
        }

        public void Log(LogType type, string format, params object[] args)
        {
            var logLine = new LogLine()
            {
                Type = type,
                Format = format,
                Args = args,
                DateTime = DateTime.Now,
            };

            string str = args?.Length > 0 ? string.Format(format, args) : format;

            logLine.DefaultLine = string.Format("{0} {1,-5}: {2}", logLine.DateTime, type, str);

            /*
            if (string.IsNullOrWhiteSpace(Name))
            {
                logLine.DefaultLine = string.Format("{0} {1,-5}: {2}", logLine.DateTime, type, str);
            }
            else
            {
                logLine.DefaultLine = string.Format("{0} {1,-5}: {3}: {2}", logLine.DateTime, type, str, Name);
            }
            */

            Log(logLine);
        }

        public ILogger WithName(string name)
        {
            var logger = Clone();
            logger.Name = name;
            return logger;
        }

        protected abstract void Log(LogLine logLine);
        protected abstract LoggerBase Clone();

        protected class LogLine
        {
            public DateTime DateTime { get; set; }
            public LogType Type { get; set; }
            public string Format { get; set; }
            public object[] Args { get; set; }
            public string DefaultLine { get; set; }
        }
    }

    public class ConsoleLogger : LoggerBase
    {
        protected override void Log(LogLine logLine)
        {
            Console.WriteLine(logLine.DefaultLine);
        }

        protected override LoggerBase Clone()
        {
            return new ConsoleLogger();
        }
    }

    public class ConsoleAndFileLogger : LoggerBase
    {
        static object s_mutex = new object();
        string m_filename;
        string m_filename2;

        public ConsoleAndFileLogger(string filename)
        {
            m_filename = filename;
            m_filename2 = m_filename + ".info.log";
        }

        protected override void Log(LogLine logLine)
        {
            Console.WriteLine(logLine.DefaultLine);
            try
            {
                using (var stream = new System.IO.StreamWriter(m_filename, true))
                {
                    stream.WriteLine(logLine.DefaultLine);
                }

                if (logLine.Type != LogType.Debug)
                {
                    using (var stream = new System.IO.StreamWriter(m_filename2, true))
                    {
                        stream.WriteLine(logLine.DefaultLine);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        protected override LoggerBase Clone()
        {
            return new ConsoleAndFileLogger(m_filename);
        }
    }
}
