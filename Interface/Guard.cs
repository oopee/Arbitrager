using System;
using System.Collections.Generic;
using System.Text;

namespace Interface
{
    public class Guard
    {
        public static void IsTrue(bool condition, string errormsg)
        {
            if (!condition)
            {
                throw new InvalidOperationException(errormsg);
            }
        }
    }
}
