using System;

namespace GameCenterAdNotifier.Common
{
    public class Utilities
    {
        public static void WriteLineWithTime(string message)
        {
            Console.WriteLine($"[{DateTime.Now}]: {message}");
        }
    }
}