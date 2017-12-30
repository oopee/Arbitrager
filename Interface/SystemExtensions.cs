using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    public static class Extensions
    {
        public static StringBuilder AppendLine(this StringBuilder builder, string format, params object[] args)
        {
            if (args?.Length > 0)
            {
                format = string.Format(format, args);
            }

            return builder.AppendLine(format);
        }
    }
}