using System.Collections.Generic;
using System.Linq;

namespace SymbolParser
{
    public class Member
    {
        public NamedCppType data;

        public Member(string line, List<ParsedTypedef> typedefs) 
        {
            data = new NamedCppType(prepareType(line), typedefs);
        }

        private static string prepareType(string original)
        {
            original = original.Replace(";", "");

            while (original.Contains(" *"))
            {
                original = original.Replace(" *", "* ");
            }

            while (original[0] == ' ')
            {
                original = original.Substring(1);
            }

            return original;
        }
    };

    public class ParsedStruct
    {
        public List<Member> members { get; private set; }
        public string name { get; private set; }
        public List<string> inheritsFrom { get; private set; }
        public ParsedAttributes attributes { get; private set; }

        public ParsedStruct(List<string> lines, List<ParsedTypedef> typedefs)
        {
            members = new List<Member>();
            inheritsFrom = new List<string>();

            string line = lines[0];
            attributes = new ParsedAttributes(line);

            line = string.Join(" ", line.Split(' ').Where(str => !str.Contains("__attribute__")));
            line = SymbolParser.handleTemplatedName(SymbolParser.preprocessTemplate(line));

            if (line.Contains(":"))
            {
                // We inherit from something.
                string[] split = line.Split(':');
                line = split[0];

                string[] multipleInheritance = split[1].Split(',');

                foreach (string inh in multipleInheritance)
                {
                    inheritsFrom.Add(inh.Replace(" ", ""));
                }
            }

            name = line.Replace("struct", "").Trim();
            int placeholderCount = 0;

            for (int i = 1; i < lines.Count; ++i)
            {
                line = lines[i];

                if (line.Contains('{') || line.Contains('}'))
                {
                    continue;
                }

                string[] split = line.Split(' ');

                foreach (string section in split)
                {
                    if (section.Contains("**_vptr"))
                    {
                        line = "void** m_vtable";
                        break;
                    }
                    else if (section.Contains('(') && !section.Contains("__attribute__"))
                    {
                        line = "void** m_funcPtrPlaceholder__" + placeholderCount.ToString();
                        ++placeholderCount;
                        break;
                    }
                }

                members.Add(new Member(line.TrimStart(), typedefs));
            }
        }
    }
}
