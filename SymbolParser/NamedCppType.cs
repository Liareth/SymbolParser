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
            m_representation = rawType;

            var typeSplit = rawType.Split(' ');

            // We should always have two spaces here -- [0] is type, [1] is name and optionally array size.
            string unprocessedType = typeSplit[0];
            string unprocessedName = typeSplit[1];

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
        }

        public override string ToString()
        {
            return m_representation;
        }
    }
}
