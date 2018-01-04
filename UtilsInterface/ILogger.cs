using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public enum LogType
    {
        Debug,
        Info,
        Error
    }

    public interface ILogger
    {
        void Debug(string format, params object[] args);
        void Info(string format, params object[] args);
        void Error(string format, params object[] args);
        void Log(LogType type, string format, params object[] args);
        ILogger WithName(string name);
        string Name { get; }
    }

    public static class Logger
    {
        public static ILogger StaticLogger { get; set; }

        public static void Error(string format, params object[] args)
        {
            StaticLogger.Log(LogType.Error, format, args);
        }

        public static void Info(string format, params object[] args)
        {
            StaticLogger.Log(LogType.Info, format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            StaticLogger.Log(LogType.Debug, format, args);
        }
    }
}

namespace System
{
    public static class ILoggerExtensions
    {
        public static Interface.ILogger WithName(this Interface.ILogger logger, params string[] names)
        {
            return logger.WithName(string.Join(".", names));
        }

        public static Interface.ILogger WithAppendName(this Interface.ILogger logger, string name)
        {
            var n = string.IsNullOrWhiteSpace(logger.Name) ? name : string.Format("{0}.{1}", logger.Name, name);
            return logger.WithName(n);
        }
    }
}
