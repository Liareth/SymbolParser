using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SymbolParser
{
    public enum BuiltInCppTypes
    {
        INT8,
        UNSIGNED_INT8,
        SIGNED_INT8,
        INT16,
        UNSIGNED_INT16,
        SIGNED_INT16,
        INT32,
        UNSIGNED_INT32,
        SIGNED_INT32,
        INT64,
        UNSIGNED_INT64,
        SIGNED_INT64,
        CHAR,
        SIGNED_CHAR,
        UNSIGNED_CHAR,
        WCHAR_T,
        VOID,
        BOOL,
        FLOAT,
        DOUBLE,
        FUNCTION,
    };

    public class CppType
    {
        public bool isPointer { get; private set; }
        public uint pointerDepth { get; private set; }
        public bool isArray { get; private set; }
        public uint arraySize { get; private set; }
        public bool isReference { get; private set; }
        public bool isConst { get; private set; }
        public bool isConstPointer { get; private set; }
        public bool isBitfield { get; private set; }
        public uint bitfieldSize { get; private set; }

        public bool isBaseType
        {
            get { return baseType.HasValue; }
        }

        public string type { get; private set; }
        public BuiltInCppTypes? baseType { get; private set; }

        public CppType(string rawType)
        {
            rawType = rawType.Replace("::", "");

            // If there are brackets in the type, it's a function pointer, so let's set our stuff manually.
            // This should be improved in the future to properly support function pointers.
            if (rawType.Contains('(') || rawType.Contains(')'))
            {
                baseType = BuiltInCppTypes.FUNCTION;
                type = getType(baseType.Value);
                isPointer = true;
                pointerDepth = 1;
                isArray = false;
                arraySize = 0;
                isConst = false;
                isConstPointer = false;
                isReference = false;
            }
            else
            {
                isPointer = getIsPointer(rawType);

                if (isPointer)
                {
                    pointerDepth = getPointerDepth(rawType);
                }

                isArray = getIsArray(rawType);

                if (isArray)
                {
                    arraySize = getArraySize(rawType);
                }

                isBitfield = getIsBitfield(rawType);

                if (isBitfield)
                {
                    bitfieldSize = getBitfieldSize(rawType);
                    rawType = rawType.Split(':')[0].Trim();
                }

                isReference = getIsReference(rawType);
                isConst = getIsConst(rawType);
                isConstPointer = getIsConstPointer(rawType);

                baseType = getBaseType(rawType);

                type = baseType.HasValue
                            ? getType(baseType.Value)
                            : getType(rawType);
            }
        }

        public string toStringRepresentation(string varName = null)
        {
            var sb = new StringBuilder();

            if (isConst)
            {
                sb.Append("const ");
            }

            sb.Append(type);

            if (isReference)
            {
                sb.Append("&");
            }

            if (isPointer)
            {
                for (var i = 0; i < pointerDepth; ++i)
                {
                    sb.Append("*");
                }

                if (isConstPointer)
                {
                    sb.Append(" const");
                }
            }

            if (varName != null)
            {
                sb.Append(" ");
                sb.Append(varName);
            }

            if (isArray)
            {
                sb.Append('[');
                sb.Append(arraySize.ToString());
                sb.Append(']');
            }

            if (isBitfield)
            {
                sb.Append(" : ");
                sb.Append(bitfieldSize.ToString());
            }

            return sb.ToString();
        }

        // Converts an array to a pointer type:
        // char[50] -> char*
        // char[50][50] -> char**
        public static string convertArrayToPtr(string line)
        {
            int leftBracketIndex = line.IndexOf('[');

            while (leftBracketIndex != -1)
            {
                int rightBracketIndex = line.IndexOf(']', leftBracketIndex);

                if (rightBracketIndex != -1)
                {
                    rightBracketIndex += 1; // +1 to include the right bracket
                    line = line.Replace(line.Substring(leftBracketIndex, rightBracketIndex - leftBracketIndex), "*"); 
                }

                leftBracketIndex = line.IndexOf('[', rightBracketIndex);
            }

            return line;
        }

        public static string convertEnumToInt(string line)
        {
            // Some symbols come with enums. We need to strip them and just assume int.
            int indexOfEnum = line.IndexOf("enum  ");

            while (indexOfEnum != -1)
            {
                int indexOfRightBracket = line.IndexOf(')');
                int indexOfComma = line.IndexOf(',', indexOfEnum);

                bool rightBracket = indexOfRightBracket != -1;
                bool comma = indexOfComma != -1;

                if (rightBracket && comma)
                {
                    if (indexOfRightBracket > indexOfComma)
                    {
                        rightBracket = false;
                    }
                    else
                    {
                        comma = false;
                    }
                }

                if (rightBracket)
                {
                    line = line.Replace(line.Substring(indexOfEnum, indexOfRightBracket - indexOfEnum), "int");
                }
                else
                {
                    line = line.Replace(line.Substring(indexOfEnum, indexOfComma - indexOfEnum), "int");
                }

                indexOfEnum = line.IndexOf("enum  ");
            }

            return line;
        }

        private static bool getIsArray(string type)
        {
            return type.Split('[', ']').Length > 1;
        }

        private static uint getArraySize(string type)
        {
            return uint.Parse(type.Split('[', ']')[1]);
        }

        private static bool getIsBitfield(string type)
        {
            return type.Split(':').Length > 1;
        }

        private static uint getBitfieldSize(string type)
        {
            return uint.Parse(type.Split(':')[1].Trim());
        }

        private static string cleanType(string type)
        {
            type = type.Replace("*", "");
            type = type.Replace("&", "");
            type = type.Replace("const", "");
            type = type.Replace("^", " ");

            int bracket = type.IndexOf('[');

            if (bracket > 0)
            {
                type = type.Substring(0, bracket);
            }

            return type.Trim();
        }

        private static bool getIsPointer(string type)
        {
            return type.Contains("*");
        }

        private static uint getPointerDepth(string type)
        {
            return (uint) type.Count(s => s == '*');
        }

        private static bool getIsReference(string type)
        {
            foreach (char ch in type)
            {
                if (ch == '&')
                {
                    return true;
                }

                if (ch == '*')
                {
                    break;
                }
            }

            return false;
        }

        private static bool getIsConst(string type)
        {
            return type.Split('*')[0].Contains("const");
        }

        private static bool getIsConstPointer(string type)
        {
            string[] split = type.Split('*');

            for (var i = 1; i < split.Length; ++i)
            {
                if (split[i].Contains("const"))
                {
                    return true;
                }
            }

            return false;
        }

        private static string getType(string type)
        {
            // User types can't have whitespace.
            return cleanType(type).Replace(" ", "");
        }

        private static string getType(BuiltInCppTypes type)
        {
            switch (type)
            {
                case BuiltInCppTypes.INT8:
                case BuiltInCppTypes.SIGNED_INT8:
                    return "int8_t";
                case BuiltInCppTypes.UNSIGNED_INT8:
                    return "uint8_t";
                case BuiltInCppTypes.INT16:
                case BuiltInCppTypes.SIGNED_INT16:
                    return "int16_t";
                case BuiltInCppTypes.UNSIGNED_INT16:
                    return "uint16_t";
                case BuiltInCppTypes.INT32:
                case BuiltInCppTypes.SIGNED_INT32:
                    return "int32_t";
                case BuiltInCppTypes.UNSIGNED_INT32:
                    return "uint32_t";
                case BuiltInCppTypes.INT64:
                case BuiltInCppTypes.SIGNED_INT64:
                    return "int64_t";
                case BuiltInCppTypes.UNSIGNED_INT64:
                    return "uint64_t";
                case BuiltInCppTypes.CHAR:
                    return "char";
                case BuiltInCppTypes.SIGNED_CHAR:
                    return "signed char";
                case BuiltInCppTypes.UNSIGNED_CHAR:
                    return "unsigned char";
                case BuiltInCppTypes.WCHAR_T:
                    return "wchar_t";
                case BuiltInCppTypes.VOID:
                    return "void";
                case BuiltInCppTypes.BOOL:
                    return "bool";
                case BuiltInCppTypes.FLOAT:
                    return "float";
                case BuiltInCppTypes.DOUBLE:
                    return "double";
                case BuiltInCppTypes.FUNCTION:
                    return "void";
            }

            Debug.Assert(false, "Could not construct a string representation for type " + type.ToString());
            return null;
        }

        private static BuiltInCppTypes? getBaseType(string type)
        {
            // Little hack ... things that are BOOL from IDA are actually 4 bytes.
            // This fucks with GCC which uses 1-byte bools.
            if (type == "BOOL")
            {
                type = "uint32_t";
            }
       
            // Strip everything that might mess with this.
            type = cleanType(type).ToLower();

            switch (type)
            {
                case "__int8":
                case "int8_t":
                case "int8":
                    return BuiltInCppTypes.INT8;
                case "unsigned __int8":
                case "unsigned^__int8":
                case "uint8_t":
                case "uint8":
                case "byte":
                    return BuiltInCppTypes.UNSIGNED_INT8;
                case "signed __int8":
                case "signed^__int8":
                    return BuiltInCppTypes.SIGNED_INT8;
                case "__int16":
                case "int16_t":
                case "int16":
                case "short":
                case "short int":
                case "short^int":
                    return BuiltInCppTypes.INT16;
                case "unsigned __int16":
                case "unsigned^__int16":
                case "uint16_t":
                case "uint16":
                case "unsigned short":
                case "unsigned short int":
                case "unsigned^short":
                case "unsigned^short^int":
                case "word":
                    return BuiltInCppTypes.UNSIGNED_INT16;
                case "signed __int16":
                case "signed^__int16":
                case "signed short":
                case "signed short int":
                case "signed^short":
                case "signed^short^int":
                    return BuiltInCppTypes.SIGNED_INT16;
                case "__int32":
                case "int32_t":
                case "int32":
                case "int":
                case "long":
                case "long int":
                case "long^int":
                    return BuiltInCppTypes.INT32;
                case "unsigned __int32":
                case "unsigned^__int32":
                case "uint32_t":
                case "uint32":
                case "unsigned":
                case "unsigned int":
                case "unsigned^int":
                case "unsigned long":
                case "unsigned long int":
                case "unsigned^long":
                case "unsigned^long^int":
                case "dword":
                    return BuiltInCppTypes.UNSIGNED_INT32;
                case "signed __int32":
                case "signed^__int32":
                case "signed int":
                case "signed^int":
                case "signed long":
                case "signed long int":
                case "signed^long":
                case "signed^long^int":
                    return BuiltInCppTypes.SIGNED_INT32;
                case "__int64":
                case "int64_t":
                case "int64":
                case "long long":
                case "long long int":
                case "long^long":
                case "long^long^int":
                    return BuiltInCppTypes.INT64;
                case "unsigned __int64":
                case "unsigned^__int64":
                case "uint64_t":
                case "uint64":
                case "unsigned long long":
                case "unsigned long long int":
                case "unsigned^long^long":
                case "unsigned^long^long^int":
                case "dword64":
                    return BuiltInCppTypes.UNSIGNED_INT64;
                case "signed __int64":
                case "signed^__int64":
                case "signed long long":
                case "signed long long int":
                case "signed^long^long":
                case "signed^long^long^int":
                    return BuiltInCppTypes.SIGNED_INT64;
                case "char":
                    return BuiltInCppTypes.CHAR;
                case "unsigned char":
                case "unsigned^char":
                    return BuiltInCppTypes.UNSIGNED_CHAR;
                case "signed char":
                case "signed^char":
                    return BuiltInCppTypes.SIGNED_CHAR;
                case "wchar_t":
                    return BuiltInCppTypes.WCHAR_T;
                case "void":
                    return BuiltInCppTypes.VOID;
                case "bool":
                    return BuiltInCppTypes.BOOL;
                case "double":
                    return BuiltInCppTypes.DOUBLE;
                case "float":
                    return BuiltInCppTypes.FLOAT;
            }

            return null;
        }

        public override string ToString()
        {
            return toStringRepresentation();
        }
    }
}