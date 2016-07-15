namespace SymbolParser
{
    public class NamedCppType
    {
        private readonly string m_representation;
        public string name { get; private set; }
        public CppType type { get; private set; }

        public NamedCppType(string rawType)
        {
            rawType = SymbolParser.handleTemplatedName(SymbolParser.preprocessTemplate(rawType));

            // Discard bitfield data for now.
            var typeSplit = rawType.Split(':')[0].Trim().Split(' ');

            string unprocessedType = "";
            for (int i = 0; i < typeSplit.Length - 1; ++i)
            {
                unprocessedType += typeSplit[i] + " ";
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
            type = new CppType(unprocessedType);

            m_representation = type.ToString() + " " + name;

            if (type.isArray)
            {
                m_representation += "[" + type.arraySize + "]";
            }
        }

        public override string ToString()
        {
            return m_representation;
        }
    }
}
