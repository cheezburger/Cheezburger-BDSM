using System;

namespace Cheezburger.SchemaManager
{
    public static class Log
    {
        public static Action<string> Logger = Console.WriteLine;

        public static void Write(Exception exception)
        {
            Logger("Error: " + exception.Message);
        }

        public static void Write(string message)
        {
            Logger(message);
        }
    }
}