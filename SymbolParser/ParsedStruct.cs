using System;
using System.Collections.Generic;
using System.Linq;

namespace SymbolParser
{
    public class Member
    {
        public NamedCppType data;

        public Member(string line) 
        {
            data = new NamedCppType(prepareType(line));
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

            if (original.Contains("**_vptr"))
            {
                original = "void** vtable_dummy";
            }
;
            return original;
        }
    };

    public class ParsedStruct
    {
        public List<Member> members { get; private set; }
        public string name { get; private set; }
        public List<string> inheritsFrom { get; private set; }
        public ParsedAttributes attributes { get; private set; }

        public ParsedStruct(List<string> lines)
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

            for (int i = 1; i < lines.Count; ++i)
            {
                line = lines[i];

                if (line.Contains("{") || line.Contains("}"))
                {
                    continue;
                }

                if (line.Contains("/*"))
                {
                    // This is a comment -- skip to next line;
                    ++i;
                    line = i < lines.Count ? lines[i] : "";
                }

                members.Add(new Member(line));
            }
        }
    }
}
