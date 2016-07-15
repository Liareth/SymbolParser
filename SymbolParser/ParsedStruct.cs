using System;
using System.Collections.Generic;

namespace SymbolParser
{
    public class Member
    {
        public string comment = "";
        public NamedCppType data;

        public Member(NamedCppType newData)
        {
            data = newData;
        }

        public Member(string newComment, NamedCppType newData)
        {
            comment = newComment;
            data = newData;
        }

        public override string ToString()
        {
            return String.IsNullOrWhiteSpace(comment) ? data.ToString() : data.ToString() + " // " + comment;
        }
    };

    public class ParsedStruct
    {
        public List<Member> members { get; private set; }
        public string name { get; private set; }
        public List<string> inheritsFrom { get; private set; }
        public string comment { get; private set; }

        public ParsedStruct(List<string> lines)
        {
            members = new List<Member>();
            inheritsFrom = new List<string>();

            string line = lines[0];
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
                    // This is a comment -- skip to next line, attaching this comment to it.
                    members.Add(new Member(prepareComment(line), new NamedCppType(prepareType(lines[i + 1]))));
                    ++i;
                }
                else
                {
                    members.Add(new Member(new NamedCppType(prepareType(line))));
                }
            }
        }

        private static string prepareComment(string original)
        {
            return original.Replace("/*", "").Replace("*/", "").Replace(" ", "");
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
                original = "void* vtable_dummy";
            }

            return original;
        }

        public override string ToString()
        {
            return String.IsNullOrWhiteSpace(comment) ? name : name + " // " + comment;
        }
    }
}
