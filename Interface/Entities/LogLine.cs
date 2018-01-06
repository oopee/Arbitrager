using System;
using System.Collections.Generic;

namespace Interface.Entities
{
    /// <summary>
    /// Simple dummy item for displaying how references work
    /// </summary>
    public class LogItem : EntityBase 
    {
        /// <summary>
        /// EF knows that this is the correct key for the LogLine property but 
        /// does not yet create foreign key references
        /// </summary>
        public int LogLineId { get; set; }

        // Dummy data
        public int ItemNumber { get; set; }


        /// <summary>
        /// This makes the line itself accessible from entity framework
        /// </summary>
        public virtual LogLine LogLine { get; set; }
    }


    public class LogLine : EntityBase
    {
        public string Message { get; set; }
        public LogType Type { get; set; }

        /// <summary>
        /// This makes Line Items accessible from entity framework
        /// Also supports adding items to lines etc. 
        /// </summary>
        public virtual List<LogItem> Items { get; set; }

        public enum LogType 
        {
            Test
        }
    }
}
