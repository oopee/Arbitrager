using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public static class TimeService
    {
        public static IClock Clock { get; set; } = new DefaultClock();
        public static DateTime UtcNow => Clock.UtcNow;
    }

    public class DefaultClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
