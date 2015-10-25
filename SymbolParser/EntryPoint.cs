using System;

namespace SymbolParser
{
    public class EntryPoint
    {
        public static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
                CommandLine.parseArgs(args);
                var parser = new SymbolParser();
#if !DEBUG
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse the file because: " + ex.Message);
                Console.Read();
            }
#endif
        }
    }
}
