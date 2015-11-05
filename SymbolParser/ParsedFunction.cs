using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SymbolParser
{
    public enum FuncAccessLevel
    {
        PUBLIC,
        PROTECTED,
        PRIVATE
    };

    public enum FuncCallingConvention
    {
        CDECL,
        THISCALL,
        STDCALL,
        FASTCALL
    };

    public class ParsedFunction
    {
        public const string DEFAULT_RET_TYPE = "void";
        // The class-specific stuff.
        public ParsedClass parentClass { get; private set; }
        public FuncAccessLevel? accessLevel { get; private set; }
        public bool isConstructor { get; private set; }
        public bool isDestructor { get; private set; }
        public bool isVirtual { get; private set; }
        public bool isConst { get; private set; }
        // The general function stuff.
        public string name { get; private set; }
        public string friendlyName { get; set; }
        public CppType returnType { get; private set; }
        public List<CppType> parameters { get; private set; }
        public FuncCallingConvention? callingConvention { get; private set; }
        public UInt32 address { get; private set; }
        public bool isStatic { get; private set; }

        public ParsedFunction(ParsedLine line, ParsedClass theClass = null)
        {
            name = SymbolParser.handleTemplatedName(line.functionName);

            if (theClass != null)
            {
                parentClass = theClass;
                accessLevel = stringToAccessLevel(line.accessLevel);

                int templateIndex = theClass.name.IndexOf("Templated", StringComparison.Ordinal);

                if (templateIndex != -1)
                {
                    // This is a template. Let's check for template ctor/dtor names.
                    
                    string className = parentClass.name.Substring(0, templateIndex);

                    if (name == className)
                    {
                        name = theClass.name;
                        isConstructor = true;
                    }
                    else if (name == "~" + className)
                    {
                        name = theClass.name;
                        isDestructor = true;
                    }
                }

                if (name == parentClass.name)
                {
                    isConstructor = true;
                }
                else if (name == "~" + parentClass.name)
                {
                    isDestructor = true;
                }

                isVirtual = line.isVirtual;
                isConst = line.isConst;
            }

            isStatic = line.isStatic;

            friendlyName = makeFriendlyName();

            if (line.returnType != null)
            {
                returnType = new CppType(SymbolParser.handleTemplatedName(line.returnType));
            }

            parameters = new List<CppType>();

            foreach (string parameter in line.parameters.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
            {
                parameters.Add(new CppType(SymbolParser.handleTemplatedName(parameter)));
            }

            callingConvention = stringToCallingConv(line.callingConvention);
            address = Convert.ToUInt32(line.address, 16);
        }

        public void crossReferenceUsing(ParsedFunction otherFunc)
        {
            if (returnType == null)
            {
                returnType = otherFunc.returnType;
            }

            if (!accessLevel.HasValue)
            {
                accessLevel = otherFunc.accessLevel;
            }
        }

        public List<String> asDeclaration()
        {
            return new List<String>
            {
                "// " + ToString(),
                String.Format("{0} = {1};", standaloneSig(), addressAsString())
            };
        }

        public List<String> asClassDeclaration()
        {
            return new List<String>
            {
                classSigHeader() + ";"
            };
        }

        public List<String> asClassDefinition()
        {
            if (CommandLine.args.target == CommandLineArgs.WINDOWS)
            {
                return new List<String>
                {
                    classSigSource(),
                    "{",
                    "    _asm",
                    "    {",
                    "        leave;",
                    "        mov eax, " + addressAsString() + ";",
                    "        jmp eax;",
                    "    };",
                    "}"
                };
            }
            else
            {
                return new List<String>
                {
                    classSigSource(),
                    "{",
                    "    __asm__ __volatile__",
                    "    (",
                    "        \"leave;\"",
                    "        \"jmp *%0;\"",
                    "        : // No outputs",
                    "        : \"r\" (" + addressAsString() + ")",
                    "        : // No clobbered registers",
                    "    );",
                    "}"
                };
            }
        }

        public static FuncAccessLevel? stringToAccessLevel(string accessLevel)
        {
            switch (accessLevel)
            {
                case "public":
                    return FuncAccessLevel.PUBLIC;
                case "protected":
                    return FuncAccessLevel.PROTECTED;
                case "private":
                    return FuncAccessLevel.PRIVATE;
            }

            return null;
        }

        public static FuncCallingConvention? stringToCallingConv(string convention)
        {
            switch (convention)
            {
                case "cdecl":
                    return FuncCallingConvention.CDECL;
                case "thiscall":
                    return FuncCallingConvention.THISCALL;
                case "fastcall":
                    return FuncCallingConvention.FASTCALL;
                case "stdcall":
                    return FuncCallingConvention.STDCALL;
            }

            return null;
        }

        public static string callingConvToString(FuncCallingConvention? convention)
        {
            if (CommandLine.args.target == CommandLineArgs.WINDOWS)
            {
                switch (convention)
                {
                    case FuncCallingConvention.CDECL:
                        return "__cdecl";

                    case FuncCallingConvention.THISCALL:
                        return "__thiscall";

                    case FuncCallingConvention.FASTCALL:
                        return "__fastcall";

                    case FuncCallingConvention.STDCALL:
                        return "__stdcall";
                }
            }
            else
            {
                switch (convention)
                {
                    case FuncCallingConvention.CDECL:
                        return "__attribute__((cdecl))";

                    case FuncCallingConvention.THISCALL:
                        return "__attribute__((thiscall))";

                    case FuncCallingConvention.FASTCALL:
                        return "__attribute__((fastcall))";

                    case FuncCallingConvention.STDCALL:
                        return "__attribute__((stdcall))";
                }
            }

            return null;
        }

        private string addressAsString()
        {
            return String.Format("0x{0:X8}", address);
        }

        private string makeFriendlyName()
        {
            string friendlyNameRet = name;

            if (parentClass != null)
            {
                if (isConstructor)
                {
                    friendlyNameRet += "Ctor";
                }
                else if (isDestructor)
                {
                    friendlyNameRet = friendlyNameRet.Replace("~", "") + "Dtor";
                }
            }

            switch (name)
            {
                case "operator+":
                    friendlyNameRet = "OperatorAddition";
                    break;
                case "operator+=":
                    friendlyNameRet = "OperatorAdditionAssignment";
                    break;
                case "operator-":
                    friendlyNameRet = "OperatorSubtraction";
                    break;
                case "operator-=":
                    friendlyNameRet = "OperatorSubtractionAssignment";
                    break;
                case "operator/":
                    friendlyNameRet = "OperatorDivision";
                    break;
                case "operator/=":
                    friendlyNameRet = "OperatorDivisionAssignment";
                    break;
                case "operator*":
                    friendlyNameRet = "OperatorMultiplication";
                    break;
                case "operator*=":
                    friendlyNameRet = "OperatorMultiplicationAssignment";
                    break;
                case "operator==":
                    friendlyNameRet = "OperatorEqualTo";
                    break;
                case "operator!=":
                    friendlyNameRet = "OperatorNotEqualTo";
                    break;
                case "operator>":
                    friendlyNameRet = "OperatorGreaterThan";
                    break;
                case "operator>=":
                    friendlyNameRet = "OperatorGreaterThanOrEqualTo";
                    break;
                case "operator<":
                    friendlyNameRet = "OperatorLesserThan";
                    break;
                case "operator<=":
                    friendlyNameRet = "OperatorLesserThanOrEqualTo";
                    break;
                case "operator%":
                    friendlyNameRet = "OperatorModulus";
                    break;
                case "operator=":
                    friendlyNameRet = "OperatorAssignment";
                    break;
                case "operator[]":
                    friendlyNameRet = "OperatorSubscript";
                    break;
                case "operator->":
                    friendlyNameRet = "OperatorDereference";
                    break;
                default:
                    if (name.Contains("operator"))
                    {
                        friendlyNameRet = "OperatorUndefined";
                    }
                    break;
            }

            return friendlyNameRet;
        }

        private string getCorrectReturnType()
        {
            if (returnType != null)
            {
                return returnType.ToString();
            }

            if (isConstructor || isDestructor)
            {
                return null;
            }

            return DEFAULT_RET_TYPE;
        }

        private string getCorrectReturnTypeIgnoreCtors()
        {
            string retType = getCorrectReturnType();

            if (retType == null)
            {
                return DEFAULT_RET_TYPE;
            }
            else
            {
                return retType;
            }
        }

        private string decorativeSig()
        {
            string classSource = classSigSource();

            if (accessLevel.HasValue)
            {
                classSource = accessLevel.ToString().ToLower() + " " + classSource;
            }

            return classSource;
        }

        private string standaloneSig()
        {
            var sb = new StringBuilder();

            sb.Append("constexpr uintptr_t ");

            if (parentClass != null)
            {
                sb.Append(parentClass.name);
                sb.Append("__");
            }

            sb.Append(friendlyName);
            return sb.ToString();
        }

        private string classSigHeader()
        {
            var sb = new StringBuilder();

            if (isStatic)
            {
                sb.Append("static ");
            }

            if (isVirtual)
            {
                sb.Append("virtual ");
            }

            string sig = baseSig();

            if (CommandLine.args.target == CommandLineArgs.WINDOWS)
            {
                if (callingConvention.HasValue)
                {
                    int funcIndex = sig.IndexOf(name, StringComparison.Ordinal);

                    if (funcIndex != -1)
                    {
                        sig = sig.Substring(0, funcIndex) + callingConvToString(callingConvention) + " " +
                              sig.Substring(funcIndex, sig.Length - funcIndex);
                    }
                }
            }

            sb.Append(sig);

            if (CommandLine.args.target == CommandLineArgs.LINUX)
            {
                if (callingConvention.HasValue)
                {
                    sb.Append(" " + ParsedFunction.callingConvToString(callingConvention));
                }
            }

            return sb.ToString();
        }

        private string formattedParams()
        {
            return parameters.Select(param => param.ToString()).Aggregate((first, second) => first + ", " + second);
        }

        private string classSigSource()
        {
            string headerDef = baseSig();
            // We don't need to check if this is valid, because a valid function
            // will always have a name.
            int indexOfFuncName = headerDef.IndexOf(name, StringComparison.Ordinal);
            return headerDef.Insert(indexOfFuncName, parentClass.name + "::");
        }

        private string stdFunctionSig()
        {
            return String.Format("std::function<{0}({1})>", getCorrectReturnTypeIgnoreCtors(), formattedParams());
        }

        private string baseSig()
        {
            string retType = getCorrectReturnType();

            if (retType != null)
            {
                retType += " ";
            }

            return (retType ?? "") +
                   name + "(" + formattedParams() + ")" +
                   (isConst ? " const" : "");
        }

        public override string ToString()
        {
            return decorativeSig();
        }
    }
}