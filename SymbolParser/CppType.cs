﻿using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SymbolParser
{
    public enum BuiltInCppTypes
    {
        // Platform-dependant types.
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

        // Standard types
        BOOL,
        CHAR,
        DOUBLE,
        FLOAT,
        INT,
        LONG_DOUBLE,
        LONG_INT,
        LONG_LONG_INT,
        SHORT_INT,
        SIGNED_CHAR,
        SIGNED_INT,
        SIGNED_LONG_INT,
        SIGNED_LONG_LONG_INT,
        SIGNED_SHORT_INT,
        UNSIGNED_CHAR,
        UNSIGNED_INT,
        UNSIGNED_LONG_INT,
        UNSIGNED_LONG_LONG_INT,
        UNSIGNED_SHORT_INT,
        VOID,
        WCHAR_T
    };

    public class CppType
    {
        private readonly string m_representation;
        public bool isPointer { get; private set; }
        public uint pointerDepth { get; private set; }
        public bool isReference { get; private set; }
        public bool isConst { get; private set; }
        public bool isConstPointer { get; private set; }

        public bool isBaseType
        {
            get { return baseType.HasValue; }
        }

        public string type { get; private set; }
        public BuiltInCppTypes? baseType { get; private set; }

        public CppType(string rawType)
        {
            isPointer = getIsPointer(rawType);

            if (isPointer)
            {
                pointerDepth = getPointerDepth(rawType);
            }

            isReference = getIsReference(rawType);
            isConst = getIsConst(rawType);
            isConstPointer = getIsConstPointer(rawType);
            baseType = getBaseType(rawType);

            type = baseType.HasValue
                       ? getType(baseType.Value)
                       : getType(rawType);

            m_representation = toStringRepresentation();
        }

        private static string cleanType(string type)
        {
            type = type.Replace("*", "");
            type = type.Replace("&", "");
            type = type.Replace("const", "");
            type = type.Replace("^", " ");
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
            if (CommandLine.args.target == CommandLineArgs.WINDOWS)
            {
                switch (type)
                {
                    case BuiltInCppTypes.INT8:
                        return "__int8";
                    case BuiltInCppTypes.UNSIGNED_INT8:
                        return "unsigned __int8";
                    case BuiltInCppTypes.SIGNED_INT8:
                        return "signed __int8";
                    case BuiltInCppTypes.INT16:
                        return "__int16";
                    case BuiltInCppTypes.UNSIGNED_INT16:
                        return "unsigned __int16";
                    case BuiltInCppTypes.SIGNED_INT16:
                        return "signed __int16";
                    case BuiltInCppTypes.INT32:
                        return "__int32";
                    case BuiltInCppTypes.UNSIGNED_INT32:
                        return "unsigned __int32";
                    case BuiltInCppTypes.SIGNED_INT32:
                        return "signed __int32";
                    case BuiltInCppTypes.INT64:
                        return "__int64";
                    case BuiltInCppTypes.UNSIGNED_INT64:
                        return "unsigned __int64";
                    case BuiltInCppTypes.SIGNED_INT64:
                        return "signed __int64";
                }
            }
            else
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
                }
            }

            switch (type)
            {
                case BuiltInCppTypes.BOOL:
                    return "bool";
                case BuiltInCppTypes.CHAR:
                    return "char";
                case BuiltInCppTypes.DOUBLE:
                    return "double";
                case BuiltInCppTypes.FLOAT:
                    return "float";
                case BuiltInCppTypes.INT:
                    return "int";
                case BuiltInCppTypes.LONG_DOUBLE:
                    return "long double";
                case BuiltInCppTypes.LONG_INT:
                    return "long int";
                case BuiltInCppTypes.LONG_LONG_INT:
                    return "long long";
                case BuiltInCppTypes.SHORT_INT:
                    return "short int";
                case BuiltInCppTypes.SIGNED_CHAR:
                    return "signed char";
                case BuiltInCppTypes.SIGNED_INT:
                    return "signed int";
                case BuiltInCppTypes.SIGNED_LONG_INT:
                    return "signed long int";
                case BuiltInCppTypes.SIGNED_LONG_LONG_INT:
                    return "signed long long int";
                case BuiltInCppTypes.SIGNED_SHORT_INT:
                    return "signed short int";
                case BuiltInCppTypes.UNSIGNED_CHAR:
                    return "unsigned char";
                case BuiltInCppTypes.UNSIGNED_INT:
                    return "unsigned int";
                case BuiltInCppTypes.UNSIGNED_LONG_INT:
                    return "unsigned long int";
                case BuiltInCppTypes.UNSIGNED_LONG_LONG_INT:
                    return "unsigned long long int";
                case BuiltInCppTypes.UNSIGNED_SHORT_INT:
                    return "unsigned short int";
                case BuiltInCppTypes.VOID:
                    return "void";
                case BuiltInCppTypes.WCHAR_T:
                    return "wchar_t";
            }

            Debug.Assert(false, "Could not construct a string representation for type {type}!");
            return null;
        }

        private static BuiltInCppTypes? getBaseType(string type)
        {
            // Strip everything that might mess with this.
            type = cleanType(type);

            if (CommandLine.args.target == CommandLineArgs.WINDOWS)
            {
                switch (type)
                {
                    case "__int8":
                        return BuiltInCppTypes.INT8;
                    case "unsigned __int8":
                    case "unsigned^__int8":
                        return BuiltInCppTypes.UNSIGNED_INT8;
                    case "signed __int8":
                    case "signed^__int8":
                        return BuiltInCppTypes.SIGNED_INT8;
                    case "__int16":
                        return BuiltInCppTypes.INT16;
                    case "unsigned __int16":
                    case "unsigned^__int16":
                        return BuiltInCppTypes.UNSIGNED_INT16;
                    case "signed __int16":
                    case "signed^__int16":
                        return BuiltInCppTypes.SIGNED_INT16;
                    case "__int32":
                        return BuiltInCppTypes.INT32;
                    case "unsigned __int32":
                    case "unsigned^__int32":
                        return BuiltInCppTypes.UNSIGNED_INT32;
                    case "signed __int32":
                    case "signed^__int32":
                        return BuiltInCppTypes.SIGNED_INT32;
                    case "__int64":
                        return BuiltInCppTypes.INT64;
                    case "unsigned __int64":
                    case "unsigned^__int64":
                        return BuiltInCppTypes.UNSIGNED_INT64;
                    case "signed __int64":
                    case "signed^__int64":
                        return BuiltInCppTypes.SIGNED_INT64;
                }
            }
            else
            {
                switch (type)
                {
                    case "int8_t":
                        return BuiltInCppTypes.INT8;
                    case "uint8_t":
                        return BuiltInCppTypes.UNSIGNED_INT8;
                    case "int16_t":
                        return BuiltInCppTypes.INT16;
                    case "uint16_t":
                        return BuiltInCppTypes.UNSIGNED_INT16;
                    case "int32_t":
                        return BuiltInCppTypes.INT32;
                    case "uint32_t":
                        return BuiltInCppTypes.UNSIGNED_INT32;
                    case "int64_t":
                        return BuiltInCppTypes.INT64;
                    case "uint64_t":
                        return BuiltInCppTypes.UNSIGNED_INT64;
                }
            }

            switch (type)
            {
                case "bool":
                    return BuiltInCppTypes.BOOL;
                case "char":
                    return BuiltInCppTypes.CHAR;
                case "double":
                    return BuiltInCppTypes.DOUBLE;
                case "float":
                    return BuiltInCppTypes.FLOAT;
                case "int":
                    return BuiltInCppTypes.INT;
                case "long double":
                    return BuiltInCppTypes.LONG_DOUBLE;
                case "long":
                case "long int":
                case "long^int":
                    return BuiltInCppTypes.LONG_INT;
                case "long long":
                case "long long int":
                case "long^long":
                case "long^long^int":
                    return BuiltInCppTypes.LONG_LONG_INT;
                case "short":
                case "short int":
                case "short^int":
                    return BuiltInCppTypes.SHORT_INT;
                case "signed char":
                case "signed^char":
                    return BuiltInCppTypes.SIGNED_CHAR;
                case "signed int":
                case "signed^int":
                    return BuiltInCppTypes.SIGNED_INT;
                case "signed long":
                case "signed long int":
                case "signed^long":
                case "signed^long^int":
                    return BuiltInCppTypes.SIGNED_LONG_INT;
                case "signed long long":
                case "signed long long int":
                case "signed^long^long":
                case "signed^long^long^int":
                    return BuiltInCppTypes.SIGNED_LONG_LONG_INT;
                case "signed short":
                case "signed short int":
                case "signed^short":
                case "signed^short^int":
                    return BuiltInCppTypes.SIGNED_SHORT_INT;
                case "unsigned char":
                case "unsigned^char":
                    return BuiltInCppTypes.UNSIGNED_CHAR;
                case "unsigned":
                case "unsigned int":
                case "unsigned^int":
                    return BuiltInCppTypes.UNSIGNED_INT;
                case "unsigned long":
                case "unsigned long int":
                case "unsigned^long":
                case "unsigned^long^int":
                    return BuiltInCppTypes.UNSIGNED_LONG_INT;
                case "unsigned long long":
                case "unsigned long long int":
                case "unsigned^long^long":
                case "unsigned^long^long^int":
                    return BuiltInCppTypes.UNSIGNED_LONG_LONG_INT;
                case "unsigned short":
                case "unsigned short int":
                case "unsigned^short":
                case "unsigned^short^int":
                    return BuiltInCppTypes.UNSIGNED_SHORT_INT;
                case "void":
                    return BuiltInCppTypes.VOID;
                case "wchar_t":
                    return BuiltInCppTypes.WCHAR_T;
                default:
                    return null;
            }
        }

        private string toStringRepresentation()
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
            }

            if (isConstPointer)
            {
                sb.Append(" const");
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return m_representation;
        }
    }
}