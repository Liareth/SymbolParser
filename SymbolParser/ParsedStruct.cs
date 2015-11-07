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
        public string comment { get; private set; }

        public ParsedStruct(List<string> lines)
        {
            members = new List<Member>();

            int startingIndex;

            if (lines[0].Contains("/*"))
            {
                comment = prepareComment(lines[0]);
                name = lines[1].Replace("struct ", "");
                startingIndex = 2;
            }
            else
            {
                name = lines[0].Replace("struct ", "");
                startingIndex = 1;
            }

            for (int i = startingIndex; i < lines.Count; ++i)
            {
                string line = lines[i];

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

            return original;
        }

        public override string ToString()
        {
            return String.IsNullOrWhiteSpace(comment) ? name : name + " // " + comment;
        }
    }
}
