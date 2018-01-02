using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface
{
    public enum LogType
    {
        Info,
        Error
    }

    public interface ILogger
    {
        void Info(string format, params object[] args);
        void Error(string format, params object[] args);
        void Log(LogType type, string format, params object[] args);
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
    }
}
