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
        public void Error(string format, params object[] args)
        {
            Log(LogType.Error, format, args);
        }

        public void Info(string format, params object[] args)
        {
            Log(LogType.Info, format, args);
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

            logLine.DefaultLine = string.Format("{0} {1}: {2}", logLine.DateTime, type, str);
            Log(logLine);
        }

        protected abstract void Log(LogLine logLine);

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
    }
}
