﻿using System.Collections.Generic;
using System.Linq;

namespace SymbolParser
{
    public class ParsedClass
    {
        public string name { get; private set; }
        public ParsedAttributes attributes { get; private set; }
        public List<ParsedFunction> functions { get; private set; }
        public List<NamedCppType> data { get; private set; }
        public List<ParsedClass> inherits { get; private set; }
        public List<ParsedClass> headerDependencies { get; private set; }
        public List<ParsedClass> sourceDependencies { get; private set; }
        public List<CppType> unknownDependencies { get; private set; }

        public ParsedClass(ParsedLine parsedLine)
            : this(SymbolParser.handleTemplatedName(parsedLine.className))
        {
        }

        public ParsedClass(string className)
        {
            name = className;
            functions = new List<ParsedFunction>();
            data = new List<NamedCppType>();
            inherits = new List<ParsedClass>();
            headerDependencies = new List<ParsedClass>();
            sourceDependencies = new List<ParsedClass>();
            unknownDependencies = new List<CppType>();
        }

        public void addFunctions(List<ParsedFunction> newFunctions)
        {
            functions.AddRange(newFunctions);

            // Sort the functions, so we only need to compare with the next one.
            // Generated functions -- Constructors -- destructors -- alphabetical func names.
            functions = functions.OrderByDescending(ctor => ctor.isConstructor)
                                 .ThenByDescending(dtor => dtor.isDestructor)
                                 .ThenBy(funcName => funcName.name).ToList();

            var duplicateFuncs = new List<ParsedFunction>();
            var uselessFuncs = new List<ParsedFunction>();

            for (var i = 0; i < functions.Count - 1; ++i)
            {
                // As the incoming list is pre-sorted, we only need to compare with the one
                // after this element.
                ParsedFunction thisFunction = functions[i];
                ParsedFunction nextFunction = functions[i + 1];

                if (thisFunction.name == nextFunction.name)
                {
                    if (thisFunction.parameters.Count == nextFunction.parameters.Count)
                    {
                        bool identical = true;

                        for (int j = 0; j < thisFunction.parameters.Count; ++j)
                        {
                            CppType thisType = thisFunction.parameters[j];
                            CppType nextType = nextFunction.parameters[j];

                            if (thisType.baseType != nextType.baseType || thisType.ToString() != nextType.ToString())
                            {
                                identical = false;
                                break;
                            }
                        }

                        // These functions have the same name and the same signature. This isn't possible in C++.
                        // We just need to completely discard one.
                        if (identical)
                        {
                            uselessFuncs.Add(nextFunction);
                        }
                    }

                    if (duplicateFuncs.Count == 0)
                    {
                        duplicateFuncs.Add(thisFunction);
                    }

                    duplicateFuncs.Add(nextFunction);
                }
                else
                {
                    if (duplicateFuncs.Count > 0)
                    {
                        renameDuplicates(duplicateFuncs);
                    }
                }
            }

            renameDuplicates(duplicateFuncs);

            foreach (ParsedFunction function in uselessFuncs)
            {
                functions.Remove(function);
            }
        }

        public void addData(List<NamedCppType> newData)
        {
            data.AddRange(newData);
        }

        public void addAttributes(ParsedAttributes newAttributes)
        {
            attributes = newAttributes;
        }

        public List<string> asClassSource()
        {
            var lines = new List<string>();

            foreach (ParsedFunction function in functions)
            {
                lines.AddRange(function.asClassDefinition());
                lines.Add("");
            }

            return lines;
        }

        public List<string> asHeader()
        {
            var lines = new List<string>();

            foreach (ParsedFunction function in functions)
            {
                lines.AddRange(function.asDeclaration());
            }

            return lines;
        }

        public List<string> asClassHeader()
        {
            var publicFuncs = new List<ParsedFunction>();
            var protectedFuncs = new List<ParsedFunction>();
            var privateFuncs = new List<ParsedFunction>();

            var lines = new List<string>();

            string className = "struct";

            if (attributes != null && attributes.attributes.Count != 0)
            {
                className += " " + attributes.ToString();
            }

            className += " " + name;
            lines.Add(className);

            if (inherits.Count != 0)
            {
                string inheritsLine = "    : ";

                foreach (ParsedClass inheritsFrom in inherits)
                {
                    inheritsLine += inheritsFrom.name + ", ";
                }

                lines.Add(inheritsLine.TrimEnd(new char[] { ',', ' ' }));
            }

            lines.Add("{");

            if (data.Count > 0)
            {
                lines.AddRange(data.Select(data => "    " + data.ToString() + ";").ToList());
            }

            lines.Add("};");
            lines.Add("");

            if (functions.Count != 0)
            { 
                lines.AddRange(functions.SelectMany(fnc => fnc.asClassDeclaration()));
                lines.Add("");
            }

            return lines;
        }

        private static void renameDuplicates(IList<ParsedFunction> duplicateFuncs)
        {
            for (var i = 0; i < duplicateFuncs.Count; ++i)
            {
                duplicateFuncs[i].friendlyName += "__" + i;
            }

            duplicateFuncs.Clear();
        }

        public override string ToString()
        {
            return name + " (" + functions.Count + ") ";
        }
    }
}