using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SymbolParser
{
    public class ParsedTypedef
    {
        public CppType from { get; private set; }
        public CppType to { get; private set; }

        public ParsedTypedef(string rawTypedef)
        {
            string[] lineSplit = rawTypedef.Split(' ');

            string toStr = "";

            for (int i = 1; i < lineSplit.Length - 1; ++i)
            {
                toStr += lineSplit[i] + " ";
            }

            toStr = toStr.TrimEnd();

            string fromStr = lineSplit[lineSplit.Length - 1].TrimEnd(';');

            from = new CppType(fromStr);
            to = new CppType(toStr);
        }

        public override string ToString()
        {
            return from.ToString() + " -> " + to.ToString();
        }
    }
}
