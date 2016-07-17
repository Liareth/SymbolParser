using System;
using System.Diagnostics;
using System.Reflection;

namespace SymbolParser
{
    public class CommandLineArgs
    {
        public const string LINUX = "linux";
        public const string WINDOWS = "windows";

        public string parse;
        public string crossref;
        public string structs;
        public string libNamespace = "NWNXLib";
        public string functionNamespace = "Functions";
        public string classNamespace = "API";
        public string outDir;
        public string target = LINUX;
        public bool produceUnityBuildFile = true;
        public string assertOnSizePath = null;
        public string classListPath = null;
        public bool mergeStructs = true;
    }

    public class CommandLine
    {
        private static CommandLineArgs m_args = new CommandLineArgs();
        public static CommandLineArgs args { get { return m_args; } }

        public static void parseArgs(string[] args)
        {
            foreach (string param in args)
            {
                string[] components = param.Split('=');
                Debug.Assert(components.Length == 2);

                string name = components[0].Replace("-", "");

                FieldInfo[] fields = typeof(CommandLineArgs).GetFields();

                foreach (FieldInfo field in fields)
                {
                    // This is inefficient, we shouldn't do this each time.
                    if (field.Name.ToLower() == name.ToLower())
                    {
                        string value = components[1];

                        try
                        {
                            field.SetValue(m_args, Convert.ChangeType(value, field.FieldType));
                        }
                        catch
                        {
                            // Intentionally left empty. If the parameter is invalid, just ignore it.
                        }                  
                    }
                }
            }
        }

        private CommandLine()
        {
        }
    }
}
