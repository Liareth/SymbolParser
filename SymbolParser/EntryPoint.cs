namespace SymbolParser
{
    public class EntryPoint
    {
        public static void Main(string[] args)
        {
            CommandLine.parseArgs(args);
            var parser = new SymbolParser();
        }
    }
}
