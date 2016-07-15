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
            type = new CppType(unprocessedType);
            m_representation = type.toStringRepresentation(unprocessedName);
        }

        public override string ToString()
        {
            return m_representation;
        }
    }
}
