using System;
using System.Collections.Generic;
using System.Linq;

namespace SymbolParser
{
    public class ParsedLine
    {
        public string accessLevel { get; private set; }
        public bool isStatic { get; private set; }
        public bool isVirtual { get; private set; }
        public string returnType { get; private set; }
        public string callingConvention { get; private set; }
        public string className { get; private set; }
        public string functionName { get; private set; }
        public string parameters { get; private set; }
        public bool isConst { get; private set; }
        public string address { get; private set; }

        private enum Components
        {
            ACCESS_LEVEL_COMPONENT,
            STATIC_COMPONENT,
            VIRTUAL_COMPONENT,
            CALLING_CONVENTION_COMPONENT,
            RETURN_TYPE_COMPONENT,
            CLASS_NAME_COMPONENT,
            FUNCTION_NAME_COMPONENT,
            PARAMETERS_COMPONENT,
            CONST_COMPONENT,
            ADDRESS_COMPONENT
        }

        private enum ComponentStatus
        {
            NOT_FOUND,
            DOES_NOT_EXIST,
            FOUND
        }

        private enum ParseState
        {
            PREFIX_STATE,
            PRE_PARAMETER_STATE,
            PARAMETER_STATE,
            POSTFIX_STATE,
            FINISHED
        }

        private class WritableTuple<T1, T2>
        {
            public T1 first { get; set; }
            public T2 second { get; set; }

            public WritableTuple(T1 first, T2 second)
            {
                this.first = first;
                this.second = second;
            }

            public override string ToString()
            {
                return String.Format("{0} -> {1}", first, second);
            }
        }

        public ParsedLine(string line)
        {
            // First item is the component status, and the second is the line number of the detected component.
            var lookupTable = new Dictionary<Components, WritableTuple<ComponentStatus, int>>();

            foreach (Components componentEnum in Enum.GetValues(typeof (Components)).Cast<Components>())
            {
                lookupTable[componentEnum] = new WritableTuple<ComponentStatus, int>(ComponentStatus.NOT_FOUND, 0);
            }

            var parserState = ParseState.PREFIX_STATE;

            string[] components = preprocessTemplate(line).Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            for (var componentCounter = 0; componentCounter < components.Length; ++componentCounter)
            {
                switch (parserState)
                {
                    case ParseState.PREFIX_STATE:

                        if (!handlePrefixStage(lookupTable, components, ref componentCounter))
                        {
                            // It's impossible to find access level / static / virtual / calling convention
                            // if we've reached this point. Let's reflect that.

                            if (lookupTable[Components.ACCESS_LEVEL_COMPONENT].first == ComponentStatus.NOT_FOUND)
                            {
                                lookupTable[Components.ACCESS_LEVEL_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                            }

                            if (lookupTable[Components.STATIC_COMPONENT].first == ComponentStatus.NOT_FOUND)
                            {
                                lookupTable[Components.STATIC_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                            }

                            if (lookupTable[Components.VIRTUAL_COMPONENT].first == ComponentStatus.NOT_FOUND)
                            {
                                lookupTable[Components.VIRTUAL_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                            }

                            parserState = ParseState.PRE_PARAMETER_STATE;

                            // We should decrement the counter so that the next state can check this element out.
                            --componentCounter;
                        }

                        break;

                    case ParseState.PRE_PARAMETER_STATE:

                        if (!handlePreParameterStage(lookupTable, components, ref componentCounter))
                        {
                            if (lookupTable[Components.CALLING_CONVENTION_COMPONENT].first == ComponentStatus.NOT_FOUND)
                            {
                                lookupTable[Components.CALLING_CONVENTION_COMPONENT].first =
                                    ComponentStatus.DOES_NOT_EXIST;
                            }

                            if (lookupTable[Components.RETURN_TYPE_COMPONENT].first == ComponentStatus.NOT_FOUND)
                            {
                                lookupTable[Components.RETURN_TYPE_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                            }

                            parserState = ParseState.PARAMETER_STATE;
                            --componentCounter;
                        }

                        break;

                    case ParseState.PARAMETER_STATE:

                        if (!handleParameterStage(lookupTable, components, ref componentCounter))
                        {
                            if (lookupTable[Components.CLASS_NAME_COMPONENT].first == ComponentStatus.NOT_FOUND)
                            {
                                lookupTable[Components.CLASS_NAME_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                            }

                            if (lookupTable[Components.FUNCTION_NAME_COMPONENT].first == ComponentStatus.NOT_FOUND)
                            {
                                lookupTable[Components.FUNCTION_NAME_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                            }

                            if (lookupTable[Components.PARAMETERS_COMPONENT].first == ComponentStatus.NOT_FOUND)
                            {
                                lookupTable[Components.PARAMETERS_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                            }

                            parserState = ParseState.POSTFIX_STATE;
                            // There's no need to decrement the counter here, as things are in determined order now.
                        }

                        break;

                    case ParseState.POSTFIX_STATE:

                        if (!handlePostfixStage(lookupTable, components, ref componentCounter))
                        {
                            if (lookupTable[Components.CONST_COMPONENT].first == ComponentStatus.NOT_FOUND)
                            {
                                lookupTable[Components.CONST_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                            }

                            if (lookupTable[Components.ADDRESS_COMPONENT].first == ComponentStatus.NOT_FOUND)
                            {
                                lookupTable[Components.ADDRESS_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                            }

                            parserState = ParseState.FINISHED;
                        }

                        break;

                    case ParseState.FINISHED:
                        break;
                }
            }
        }

        private bool handlePrefixStage(Dictionary<Components, WritableTuple<ComponentStatus, int>> lookupTable,
                                       string[] components,
                                       ref int currentIndex)
        {
            if (lookupTable[Components.ACCESS_LEVEL_COMPONENT].first == ComponentStatus.NOT_FOUND)
            {
                string tempAccessLevel = getAccessLevel(components, currentIndex);

                if (tempAccessLevel != null)
                {
                    accessLevel = tempAccessLevel;
                    lookupTable[Components.ACCESS_LEVEL_COMPONENT].first = ComponentStatus.FOUND;
                    lookupTable[Components.ACCESS_LEVEL_COMPONENT].second = currentIndex;
                    return true;
                }
            }

            if (lookupTable[Components.STATIC_COMPONENT].first == ComponentStatus.NOT_FOUND)
            {
                if (getIsStatic(components, currentIndex))
                {
                    isStatic = true;
                    lookupTable[Components.STATIC_COMPONENT].first = ComponentStatus.FOUND;
                    lookupTable[Components.STATIC_COMPONENT].second = currentIndex;
                    return true;
                }
            }

            if (lookupTable[Components.VIRTUAL_COMPONENT].first == ComponentStatus.NOT_FOUND)
            {
                if (getIsVirtual(components, currentIndex))
                {
                    isVirtual = true;
                    lookupTable[Components.VIRTUAL_COMPONENT].first = ComponentStatus.FOUND;
                    lookupTable[Components.VIRTUAL_COMPONENT].second = currentIndex;
                    return true;
                }
            }

            return false;
        }

        private bool handlePreParameterStage(Dictionary<Components, WritableTuple<ComponentStatus, int>> lookupTable,
                                             string[] components,
                                             ref int currentIndex)
        {
            if (lookupTable[Components.CALLING_CONVENTION_COMPONENT].first == ComponentStatus.NOT_FOUND)
            {
                string tempCallingConvention = getCallingConvention(components, currentIndex);

                if (tempCallingConvention != null)
                {
                    callingConvention = tempCallingConvention;
                    lookupTable[Components.CALLING_CONVENTION_COMPONENT].first = ComponentStatus.FOUND;
                    lookupTable[Components.CALLING_CONVENTION_COMPONENT].second = currentIndex;
                    return true;
                }
            }

            if (lookupTable[Components.RETURN_TYPE_COMPONENT].first == ComponentStatus.NOT_FOUND)
            {
                var maxIndex = 0;
                var foundCallingConvention = false;

                for (int i = currentIndex; i < components.Count(); ++i)
                {
                    if (!components[i].Contains('('))
                    {
                        continue;
                    }

                    maxIndex = i - 1;

                    if (i > 0)
                    {
                        string tempCallingConvention = getCallingConvention(components, maxIndex);

                        if (tempCallingConvention != null)
                        {
                            callingConvention = tempCallingConvention;
                            lookupTable[Components.CALLING_CONVENTION_COMPONENT].first = ComponentStatus.FOUND;
                            lookupTable[Components.CALLING_CONVENTION_COMPONENT].second = currentIndex;
                            --maxIndex;
                            foundCallingConvention = true;
                        }
                    }

                    break;
                }

                var minValues = new List<int> {0};

                if (lookupTable[Components.ACCESS_LEVEL_COMPONENT].first == ComponentStatus.FOUND)
                {
                    minValues.Add(lookupTable[Components.ACCESS_LEVEL_COMPONENT].second + 1);
                }

                if (lookupTable[Components.STATIC_COMPONENT].first == ComponentStatus.FOUND)
                {
                    minValues.Add(lookupTable[Components.STATIC_COMPONENT].second + 1);
                }

                if (lookupTable[Components.VIRTUAL_COMPONENT].first == ComponentStatus.FOUND)
                {
                    minValues.Add(lookupTable[Components.VIRTUAL_COMPONENT].second + 1);
                }

                string tempReturnType = getReturnType(components, minValues.Max(), maxIndex);

                if (tempReturnType != null)
                {
                    returnType = tempReturnType;
                    lookupTable[Components.RETURN_TYPE_COMPONENT].first = ComponentStatus.FOUND;
                    lookupTable[Components.RETURN_TYPE_COMPONENT].second = currentIndex;

                    if (foundCallingConvention)
                    {
                        currentIndex = maxIndex + 1;
                    }
                    else
                    {
                        currentIndex = maxIndex;
                    }

                    return true;
                }
            }

            return false;
        }

        private bool handleParameterStage(Dictionary<Components, WritableTuple<ComponentStatus, int>> lookupTable,
                                          string[] components,
                                          ref int currentIndex)
        {
            // At this point, the signature cannot vary -- it must be <optional class name> function name(params).
            // So we've stopped operating as a state machine at this point.

            string funcNameSearchString;
            int namespaceIndex = components[currentIndex].LastIndexOf("::", StringComparison.Ordinal);

            if (namespaceIndex == -1)
            {
                funcNameSearchString = components[currentIndex];
                lookupTable[Components.CLASS_NAME_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
            }
            else
            {
                className = components[currentIndex].Substring(0, namespaceIndex);
                List<string> templatedTypes = SymbolParser.getMatchingBrackets(className);

                // Each individual element of the matching array should represent a templated type.
                // Construct a type from the string, then extract the string representation from it.
                // This allows us to handle special cases, e.g. function pointers.
                for (int i = 0; i < templatedTypes.Count; ++i)
                {
                    string[] elements = templatedTypes[i].Split(',');

                    foreach (string element in elements)
                    {
                        className = className.Replace(element, new CppType(element).ToString());
                    }
                }

                // Replace any spaces from the ToString representation with our delimiter again.
                className = preprocessTemplate(className);

                // Strip any additional namespaces.
                className = className.Replace("::", "");
                funcNameSearchString = components[currentIndex].Substring(namespaceIndex + 2,
                                                                          components[currentIndex].Length -
                                                                          namespaceIndex - 2);
                lookupTable[Components.CLASS_NAME_COMPONENT].first = ComponentStatus.FOUND;
                lookupTable[Components.CLASS_NAME_COMPONENT].second = currentIndex;
            }

            // Let's find the name now -- it will always be followed by parens.
            int leftParenIndex = funcNameSearchString.IndexOf('(');

            if (leftParenIndex == -1)
            {
                // This isn't valid. Let's leave.
                lookupTable[Components.PARAMETERS_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                return false;
            }

            string funcName = funcNameSearchString.Substring(0, leftParenIndex);

            if (String.IsNullOrWhiteSpace(funcName))
            {
                // This isn't valid. Let's leave.
                lookupTable[Components.FUNCTION_NAME_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                return false;
            }

            functionName = funcName;
            lookupTable[Components.FUNCTION_NAME_COMPONENT].first = ComponentStatus.FOUND;
            lookupTable[Components.FUNCTION_NAME_COMPONENT].second = currentIndex;

            int rightParenIndex = funcNameSearchString.IndexOf(')');

            while (rightParenIndex == -1 && rightParenIndex < components.Length)
            {
                ++currentIndex;
                funcNameSearchString += " " + components[currentIndex];
                rightParenIndex = funcNameSearchString.IndexOf(')');
            }

            if (rightParenIndex == -1)
            {
                // This isn't valid. Let's leave.
                lookupTable[Components.PARAMETERS_COMPONENT].first = ComponentStatus.DOES_NOT_EXIST;
                return false;
            }

            // It doesn't matter if there's nothing in the parens -- as long as they are there.
            parameters = funcNameSearchString.Substring(leftParenIndex + 1, rightParenIndex - leftParenIndex - 1);
            lookupTable[Components.PARAMETERS_COMPONENT].first = ComponentStatus.FOUND;
            lookupTable[Components.PARAMETERS_COMPONENT].second = currentIndex;

            return false;
        }

        private bool handlePostfixStage(Dictionary<Components, WritableTuple<ComponentStatus, int>> lookupTable,
                                        string[] components,
                                        ref int currentIndex)
        {
            // We simply need to check for const first, and if it doesn't exist, then 
            // check for the address until the end of line.

            if (lookupTable[Components.CONST_COMPONENT].first == ComponentStatus.NOT_FOUND)
            {
                if (getIsConst(components, currentIndex))
                {
                    isConst = true;
                    lookupTable[Components.CONST_COMPONENT].first = ComponentStatus.FOUND;
                    lookupTable[Components.CONST_COMPONENT].second = currentIndex;
                    return true;
                }
            }

            if (lookupTable[Components.ADDRESS_COMPONENT].first == ComponentStatus.NOT_FOUND)
            {
                string tempAddress = null;

                while (currentIndex < components.Length)
                {
                    if (components[currentIndex].Substring(0, 2) == "0x")
                    {
                        tempAddress = components[currentIndex];
                        break;
                    }

                    currentIndex++;
                }

                if (tempAddress != null)
                {
                    address = tempAddress;
                    lookupTable[Components.ADDRESS_COMPONENT].first = ComponentStatus.FOUND;
                    lookupTable[Components.ADDRESS_COMPONENT].second = currentIndex;
                }
            }

            return false;
        }

        private static string getAccessLevel(string[] components, int currentIndex)
        {
            switch (components[currentIndex])
            {
                case "public:":
                    return "public";
                case "protected:":
                    return "protected";
                case "private:":
                    return "private";
                default:
                    return null;
            }
        }

        private static bool getIsStatic(string[] components, int currentIndex)
        {
            return components[currentIndex] == "static";
        }

        private static bool getIsVirtual(string[] components, int currentIndex)
        {
            return components[currentIndex] == "virtual";
        }

        private static string getReturnType(string[] components, int minIndex, int maxIndex)
        {
            string returnType = null;

            for (int returnTypeIndex = minIndex; returnTypeIndex <= maxIndex; ++returnTypeIndex)
            {
                if (returnType == null)
                {
                    returnType = components[returnTypeIndex];
                }
                else
                {
                    returnType += " " + components[returnTypeIndex];
                }
            }

            return returnType;
        }

        private static string getCallingConvention(string[] components, int currentIndex)
        {
            switch (components[currentIndex])
            {
                case "__thiscall":
                    return "thiscall";
                case "__cdecl":
                    return "cdecl";
                case "__stdcall":
                    return "stdcall";
                case "__fastcall":
                    return "fastcall";
                default:
                    return null;
            }
        }

        private static bool getIsConst(string[] components, int currentIndex)
        {
            return components[currentIndex] == "const";
        }

        private static string preprocessTemplate(string line)
        {
            List<string> matching = SymbolParser.getMatchingBrackets(line);
            return matching.Aggregate(line, (current, match) => current.Replace(match, match.Replace(" ", "^")));
        }

        public override string ToString()
        {
            return accessLevel + (isStatic ? " static" : "") + (isVirtual? " virtual" : "") + " " + returnType + " " +
                   callingConvention + " " + className + " " + functionName + " " + parameters + (isConst ? " const" : "") + " " + address;
        }
    }
}