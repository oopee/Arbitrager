using System;

namespace Arbitrager
{
    public static class ConsoleUtils
    {
        public static bool Confirm()
        {
            while (true)
            {
                Console.Write("Do you want to continue (Y/N)> ");
                string answer = Console.ReadLine();
                if (answer.ToLower() == "y")
                {
                    return true;
                }
                else if (answer.ToLower() == "n")
                {
                    return false;
                }
            }
        }
    }
}
