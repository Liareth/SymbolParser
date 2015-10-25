using System;

namespace SymbolParser
{
    public class EntryPoint
    {
        public static void Main(string[] args)
        {
            try
            {
                CommandLine.parseArgs(args);
                var parser = new SymbolParser();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse the file because: " + ex.Message);
                Console.Read();
            }
        }
    }
}
