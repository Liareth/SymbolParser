using System;
using System.Collections.Generic;

namespace SymbolParser
{
    public enum AttributeType
    {
        PACKED,
        ALIGNED,
        UNKNOWN
    }

    public class ParsedAttribute
    {
        AttributeType type;
        uint value;

        public ParsedAttribute(string rawType)
        {
            string attribute = rawType.Split(new string[] { "((", "))" }, StringSplitOptions.RemoveEmptyEntries)[1];

            if (attribute.Contains("packed"))
            {
                type = AttributeType.PACKED;
                value = 0;
            }
            else if (attribute.Contains("aligned"))
            {
                type = AttributeType.ALIGNED;
                value = uint.Parse(attribute.Split('(')[1]);
            }
            else
            {
                type = AttributeType.UNKNOWN;
                value = 0;
            }
        }

        public override string ToString()
        {
            if (CommandLine.args.target == CommandLineArgs.WINDOWS)
            {
                return "";
            }
            else
            {
                string typeValue = "";

                switch (type)
                {
                    case AttributeType.ALIGNED:
                        typeValue = "aligned(" + value.ToString() + ")";
                        break;

                    case AttributeType.PACKED:
                        typeValue = "packed";
                        break;

                    default:
                    case AttributeType.UNKNOWN:
                        typeValue = "UNKNOWN";
                        break;
                }

                return "__attribute__((" + typeValue + "))";
            }
        }
    }

    public class ParsedAttributes
    {
        public List<ParsedAttribute> attributes { get; private set; }

        public ParsedAttributes(string rawType)
        {
            attributes = new List<ParsedAttribute>();

            string[] typeSplit = rawType.Split(' ');

            foreach (string type in typeSplit)
            {
                if (type.Contains("__attribute__"))
                {
                    attributes.Add(new ParsedAttribute(type));
                }
            }
        }

        public override string ToString()
        {
            string attributesAsStr = "";

            foreach (ParsedAttribute attribute in attributes)
            {
                attributesAsStr += attribute.ToString() + " ";
            }

            return attributesAsStr.Trim();
        }
    }
}
