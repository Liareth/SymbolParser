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

            foreach (string parameter in line.parameters.Split(','))
            {
                CppType param = new CppType(SymbolParser.handleTemplatedName(parameter));

                if (param.isPointer || (param.baseType.HasValue && param.baseType.Value != BuiltInCppTypes.VOID))
                {
                    parameters.Add(param);
                }
            }

            callingConvention = stringToCallingConv(line.callingConvention);
            address = Convert.ToUInt32(line.address, 16);
        }

        private ParsedFunction()
        {
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
            List<string> definition = new List<string>();
            definition.Add(classSigSource());
            definition.Add("{");

            {
                string funcPtr = "    ";
                funcPtr += "using FuncPtrType = ";
                funcPtr += returnType != null ? returnType.ToString() : "void";
                funcPtr += "(" + callingConvToString(FuncCallingConvention.THISCALL) + " *)";
                funcPtr += "(";
                funcPtr += parentClass.name + "*, ";
                
                if (parameters.Count != 0)
                {
                    funcPtr += parameters.Select(param => param.ToString()).Aggregate((first, second) => first + ", " + second);
                }

                funcPtr = funcPtr.TrimEnd(new char[] { ' ', ',' }) + ");";
                definition.Add(funcPtr);
            }

            definition.Add("    FuncPtrType func = reinterpret_cast<FuncPtrType>(" + "Functions::" + parentClass.name + "__" + friendlyName + ");");


            {
                string body = "    ";

                if (returnType != null)
                {
                    body += "return ";
                }

                body += "func(thisPtr, ";

                for (int i = 0; i < parameters.Count; ++i)
                {
                    body += "a" + i.ToString() + ", ";
                }

                body = body.TrimEnd(new char[] { ' ', ',' }) + ");";

                definition.Add(body);
            }

            definition.Add("}");
            return definition;
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

        public static string callingConvToString(FuncCallingConvention convention)
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
            return getCorrectReturnType() ?? DEFAULT_RET_TYPE;
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
            return "constexpr uintptr_t " + parentClass.name + "__" + friendlyName;
        }

        private string classSigHeader()
        {
            return baseSig();
        }

        private string getFormattedParams(bool includeParameterNames = false)
        {
            if (parameters.Count == 0)
            {
                return "";
            }

            string formattedParams = "";

            for (int i = 0; i < parameters.Count; ++i)
            {
                formattedParams += parameters[i].ToString();

                if (includeParameterNames)
                {
                    formattedParams += " a" + i.ToString();
                }

                formattedParams += ", ";
            }

            formattedParams = formattedParams.TrimEnd(new char[] { ' ', ',' });
            return formattedParams;
        }

        private string classSigSource()
        {
            return baseSig(true);
        }

        private string stdFunctionSig()
        {
            return String.Format("std::function<{0}({1})>", getCorrectReturnTypeIgnoreCtors(), getFormattedParams());
        }

        private string baseSig(bool includeParameterNames = false)
        {
            string sig = getCorrectReturnTypeIgnoreCtors() + " ";
            sig += parentClass.name + "__" + friendlyName;
            sig += "(" + parentClass.name + "* thisPtr";

            string param = getFormattedParams(includeParameterNames);

            if (!String.IsNullOrWhiteSpace(param))
            {
                sig += ", " + param;
            }

            sig += ")";

            return sig;
        }

        public override string ToString()
        {
            return decorativeSig();
        }
    }
}