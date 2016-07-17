using System.Collections.Generic;
using System.Linq;

namespace SymbolParser
{
    public class NamedCppType
    {
        public string name { get; private set; }
        public CppType type { get; private set; }
        public ParsedAttributes attributes { get; private set; }

        public NamedCppType(string rawType, List<ParsedTypedef> typedefs = null)
        {
            rawType = SymbolParser.handleTemplatedName(SymbolParser.preprocessTemplate(rawType));

            attributes = new ParsedAttributes(rawType);
            rawType = string.Join(" ", rawType.Split(' ').Where(str => !str.Contains("__attribute__")));

            string[] bitFieldSplit = rawType.Split(':');
            string[] typeSplit = bitFieldSplit[0].Trim().Split(' ');

            string unprocessedType = "";
            for (int i = 0; i < typeSplit.Length - 1; ++i)
            {
                unprocessedType += typeSplit[i] + " ";
            }

            if (bitFieldSplit.Length > 1)
            {
                unprocessedType += ": " + bitFieldSplit[1].Trim();
            }

            string unprocessedName = typeSplit[typeSplit.Length-1];

            var arraySplit = unprocessedName.Split('[', ']');

            if (arraySplit.Length > 1)
            {
                for (int i = 1; i < arraySplit.Length; i += 2)
                {
                    unprocessedType = unprocessedType + '[' + arraySplit[i] + ']';
                }

                unprocessedName = unprocessedName.Substring(0, unprocessedName.IndexOf('['));
            }

            name = unprocessedName;

            // Replace reserved keywords.
            switch (name)
            {
                case "class":     name = "_class"; break;
                case "struct":    name = "_struct"; break;
                case "private":   name = "_private"; break;
                case "protected": name = "_protected"; break;
                case "public":    name = "_public"; break;
                case "new":       name = "_new"; break;
                case "delete":    name = "_delete"; break;;
                default: break;
            }

            type = new CppType(unprocessedType, typedefs);
        }

        public override string ToString()
        {
            string representation = attributes.attributes.Count != 0 ? (attributes.ToString() + " ") : "";
            return representation + type.toStringRepresentation(name);
        }
    }
}
