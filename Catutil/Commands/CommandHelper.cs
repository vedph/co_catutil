using System;

namespace Catutil.Commands
{
    internal static class CommandHelper
    {
        public static bool PromptForConfirmation()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Are you sure (Y/N)?");
            Console.ResetColor();
#if DEBUG
            return true;
#else
            string s = Console.ReadLine()?.ToLowerInvariant();
            return s == "y";
#endif
        }
    }
}
