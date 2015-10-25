using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// TASK LIST:
// 1. Improve platform dependant types (__int32). Add flag to indicate platform dependant, and don't apply these during cross referencing.
// 2. Perhaps improve the implementation of unknown types.

namespace SymbolParser
{
    public class SymbolParser
    {
        public const string OUT_CLASS_FOLDER_WINDOWS = @"ClassWrappers\Windows";
        public const string OUT_CLASS_FOLDER_LINUX = @"ClassWrappers\Linux";

        public readonly string[] blackListedPatterns =
        {
            "sub_",
            "unknown_",
            "SEH",
            "iterator'",
            "keyed to'",
            "MS_",
            "Mem5",
            "vector deleting",
            "scalar deleting",
            "SJournalEntry",
            "SMstKeyEntry",
            "SMstNameEntry",
            "SRecord",
            "SSubNetProfile",
            "flex_unit",
            "file4close",
            "i4canCreate",
            "bad_alloc",
            "bad_cast",
            "bad_exception",
            "bad_typeid",
            "exception",
            "type_info",
        };

        public readonly string[] whitelistedFreestandingPatterns =
        {
            "Exo",
            "Admin",
            "Create"
        };

        public SymbolParser()
        {
            List<string> clean = cleanFile(CommandLine.args.parse);
            List<ParsedClass> classes = getClasses(getParsedLines(clean));

            if (CommandLine.args.crossref != null)
            {
                List<ParsedClass> crossRefClasses = getClasses(getParsedLines(cleanFile(CommandLine.args.crossref)));
                crossReference(classes, crossRefClasses);
            }

            handleDependencies(classes);

            dumpStandaloneFiles(classes);
            dumpClassFiles(classes);
        }

        public static string handleTemplatedName(string templatedName)
        {
            return handleTemplatedName_r(templatedName).Item1;
        }

        public static List<string> getMatchingBrackets(string line)
        {
            var matchingBrackets = new List<string>();

            int index = line.IndexOf('<');

            while (index != -1)
            {
                ++index;
                var matchingIndex = 0;
                var count = 1;

                for (int i = index; i < line.Length; ++i)
                {
                    char at = line[i];

                    if (at == '<')
                    {
                        ++count;
                    }

                    if (at == '>')
                    {
                        --count;

                        if (count == 0)
                        {
                            matchingIndex = i;
                            break;
                        }
                    }
                }

                if (matchingIndex > index)
                {
                    matchingBrackets.Add(line.Substring(index, matchingIndex - index));
                    index = line.IndexOf('<', matchingIndex + 1);
                }
                else
                {
                    // There was no matching bracket -- probably because this is an operator.
                    break;
                }
            }

            return matchingBrackets;
        }

        private static Tuple<string, string> handleTemplatedName_r(string templatedName)
        {
            string originalName = templatedName;
            List<string> matching = getMatchingBrackets(templatedName);
            templatedName = matching.Select(handleTemplatedName_r)
                                    .Aggregate(templatedName, (current, recursiveMatch) => current
                                        .Replace("<" + recursiveMatch.Item2 + ">", "Templated" + recursiveMatch.Item1
                                        .Replace("*", "Ptr")
                                        .Replace("&", "Ref")
                                        .Replace("^", "")
                                        .Replace(",", "")));

            return new Tuple<string, string>(templatedName, originalName);
        }

        private void crossReference(List<ParsedClass> mainClasses, List<ParsedClass> secondClasses)
        {
            foreach (ParsedClass mainClass in mainClasses)
            {
                foreach (ParsedClass secondClass in secondClasses)
                {
                    if (mainClass.name == secondClass.name)
                    {
                        foreach (ParsedFunction mainFunc in mainClass.functions)
                        {
                            foreach (ParsedFunction secondFunc in secondClass.functions)
                            {
                                if (mainFunc.name == secondFunc.name)
                                {
                                    mainFunc.crossReferenceUsing(secondFunc);
                                }
                            }
                        }
                    }
                }
            }
        }

        private List<string> cleanFile(string path)
        {
            return cleanRaw(File.ReadAllLines(path).ToList());
        }

        // Provides processing on the provided file to make our life easier.
        private List<string> cleanRaw(List<string> lines)
        {
            var cleanList = new List<string>();

            Parallel.ForEach(lines, line =>
            {
                if (line[0] == '_' || blackListedPatterns.Any(line.Contains))
                {
                    return;
                }

                line = line.Replace("class ", "");
                line = line.Replace("struct ", "");
                line = line.Replace(" *", "*");
                line = line.Replace(" &", "&");
                line = line.Replace(")const", ") const");
                line = line.Replace("std::", "");
                line = CppType.convertArrayToPtr(line);
                line = CppType.convertEnumToInt(line);

                lock (cleanList)
                {
                    cleanList.Add(line);
                }
            });

            return cleanList;
        }

        private List<ParsedLine> getParsedLines(List<string> lines)
        {
            return lines.AsParallel().Select(line => new ParsedLine(line)).Where(parsedLine => parsedLine.functionName != null).ToList();
        }

        private List<ParsedClass> getClasses(List<ParsedLine> parsedLines)
        {
            var parsedClassDict = new Dictionary<string, ParsedClass>();
            var parsedFunctionDict = new Dictionary<ParsedClass, List<ParsedFunction>>();

            foreach (ParsedLine line in parsedLines)
            {
                ParsedClass thisClass = null;
                string className = line.className;

                if (className == null)
                {
                    foreach (string whitelist in whitelistedFreestandingPatterns)
                    {
                        if (line.functionName.Contains(whitelist))
                        {
                            className = ParsedClass.FREE_STANDING_CLASS_NAME;
                        }
                    }

                    if (className == null)
                    {
                        continue;
                    }
                }

                parsedClassDict.TryGetValue(handleTemplatedName(className), out thisClass);

                if (thisClass == null)
                {
                    thisClass = new ParsedClass(line);
                    parsedClassDict[thisClass.name] = thisClass;
                    parsedFunctionDict[thisClass] = new List<ParsedFunction>();
                }

                parsedFunctionDict[thisClass].Add(new ParsedFunction(line, thisClass));
            }

            List<ParsedClass> parsedClasses = parsedClassDict.Values.OrderBy(theClass => theClass.name).ToList();

            foreach (KeyValuePair<ParsedClass, List<ParsedFunction>> pair in parsedFunctionDict)
            {
                pair.Key.addFunctions(pair.Value);
            }

            return parsedClasses;
        }

        private void handleDependencies(List<ParsedClass> classes)
        {
            // Determine dependencies
            foreach (ParsedClass theClass in classes)
            {
                var dependencies = new List<CppType>();

                foreach (ParsedFunction theFunction in theClass.functions)
                {
                    if (theFunction.returnType != null && !theFunction.returnType.isBaseType)
                    {
                        dependencies.Add(theFunction.returnType);
                    }

                    dependencies.AddRange(theFunction.parameters.Where(param => !param.isBaseType));
                }

                // Sorting the list beforehand ensures we don't have to check for duplicates every time.
                dependencies.Sort((first, second) => String.CompareOrdinal(first.type, second.type));

                var needConcreteDef = false;

                for (var i = 0; i < dependencies.Count; ++i)
                {
                    CppType dependency = dependencies[i];

                    // We can't really do anything with this.
                    if (dependency.type == "..." || dependency.type == theClass.name)
                    {
                        continue;
                    }

                    if (!dependency.isPointer)
                    {
                        needConcreteDef = true;
                    }

                    if (dependencies.Count <= i + 1 || dependency.type != dependencies[i + 1].type)
                    {
                        ParsedClass dependencyClass = classes.FirstOrDefault(param => param.name == dependency.type);

                        if (dependencyClass != null)
                        {
                            if (needConcreteDef)
                            {
                                theClass.headerDependencies.Add(dependencyClass);
                            }
                            else
                            {

                                theClass.sourceDependencies.Add(dependencyClass);
                            }
                        }
                        else
                        {
                            theClass.unknownDependencies.Add(dependency);
                        }

                        needConcreteDef = false;
                    }

                }
            }
        }      

        private static void dumpStandaloneFiles(List<ParsedClass> classes)
        {
            string ns1 = CommandLine.args.libNamespace;
            string ns2 = CommandLine.args.functionNamespace;

            string funcDir = CommandLine.args.outDir;

            if (!Directory.Exists(funcDir))
            {
                Directory.CreateDirectory(funcDir);
            }

            string functionHeader = CommandLine.args.headerGuardPrefix;
            string fileName;

            if (CommandLine.args.target == CommandLineArgs.WINDOWS)
            {
                functionHeader += "FUNCTIONS_WINDOWS_HPP";
                fileName = "FunctionsWindows";
            }
            else
            {
                functionHeader += "FUNCTIONS_LINUX_HPP";
                fileName = "FunctionsLinux";
            }

            var source = new List<string>();
            var header = new List<string>();

            header.Add("#ifndef " + functionHeader);
            header.Add("#define " + functionHeader);
            source.Add("#include \"" + fileName + ".hpp\"");

            header.Add("");
            header.Add("#include <cstdint>");
            header.Add("");
            header.Add("namespace " + ns1 + " {");
            header.Add("");
            header.Add("namespace " + ns2 + " {");
            header.Add("");

            source.Add("");
            source.Add("namespace " + ns1 + " {");
            source.Add("");
            source.Add("namespace " + ns2 + " {");
            source.Add("");

            foreach (ParsedClass theClass in classes)
            {
                header.AddRange(theClass.asHeader());
                header.Add("");

                source.AddRange(theClass.asSource(ns1 + "::" + ns2));
                source.Add("");
            }

            header.Add("}");
            header.Add("");
            header.Add("}");
            header.Add("");

            source.Add("}");
            source.Add("");
            source.Add("}");
            source.Add("");

            header.Add("#endif // " + functionHeader);

            File.WriteAllLines(Path.Combine(funcDir, fileName + ".cpp"), source);
            File.WriteAllLines(Path.Combine(funcDir, fileName + ".hpp"), header);
        }

        private void dumpClassFiles(List<ParsedClass> classes)
        {
            string classDir = CommandLine.args.outDir;

            if (CommandLine.args.target == CommandLineArgs.WINDOWS)
            {
                classDir = Path.Combine(classDir, OUT_CLASS_FOLDER_WINDOWS);
            }
            else
            {
                classDir = Path.Combine(classDir, OUT_CLASS_FOLDER_LINUX);
            }

            if (!Directory.Exists(classDir))
            {
                Directory.CreateDirectory(classDir);
            }

            var source = new List<string>();
            var header = new List<string>();
            var unknownTypes = new List<CppType>();

            foreach (ParsedClass theClass in classes)
            {
                foreach (CppType unknownType in theClass.unknownDependencies.Where(unknownType => !unknownTypes.Contains(unknownType)))
                {
                    unknownTypes.Add(unknownType);
                }

                source.Clear();
                header.Clear();

                buildClassHeader(header, theClass);
                buildClassSource(source, theClass);

                File.WriteAllLines(Path.Combine(classDir, theClass.name + ".hpp"), header);
                File.WriteAllLines(Path.Combine(classDir, theClass.name + ".cpp"), source);
            }

            foreach (CppType unknownType in unknownTypes)
            {
                var headerFile = new List<string>();

                string headerGuard = CommandLine.args.headerGuardPrefix + unknownType.type.ToUpper() + "_HPP";
                headerFile.Add("#ifndef " + headerGuard);
                headerFile.Add("#define " + headerGuard);
                headerFile.Add("");
                headerFile.Add("namespace " + CommandLine.args.libNamespace + " {");
                headerFile.Add("");
                headerFile.Add("class " + unknownType.type + " { };");
                headerFile.Add("");
                headerFile.Add("}");
                headerFile.Add("");
                headerFile.Add("#endif // " + headerGuard);

                File.WriteAllLines(Path.Combine(classDir, "unknown_" + unknownType.type + ".hpp"), headerFile);
            }
        }

        private static void buildClassSource(List<string> body, ParsedClass theClass)
        {
            body.Add("#include \"" + theClass.name + ".hpp\"");
            body.Add("");

            if (theClass.sourceDependencies.Count > 0)
            {
                body.AddRange(theClass.sourceDependencies.Select(dependency => String.Format("#include \"{0}.hpp\"", dependency.name)));
                body.Add("");
            }

            body.Add("namespace " + CommandLine.args.libNamespace + " {");
            body.Add("");

            if (CommandLine.args.target == CommandLineArgs.LINUX)
            {
                body.Add("// Disable optimizations for this file.");
                body.Add("#pragma GCC push_options");
                body.Add("#pragma GCC optimize (\"O0\")");
                body.Add("");
                body.Add("// Disable no return value warning for this file.");
                body.Add("#pragma GCC diagnostic push");
                body.Add("#pragma GCC diagnostic ignored \"-Wreturn-type\"");
                body.Add("");
            }

            body.AddRange(theClass.asClassSource());

            if (CommandLine.args.target == CommandLineArgs.LINUX)
            {
                body.Add("#pragma GCC pop_options");
                body.Add("");
                body.Add("#pragma GCC diagnostic pop");
                body.Add("");
            }

            body.Add("");
            body.Add("}");
        }

        private static void buildClassHeader(List<string> header, ParsedClass theClass)
        {
            string headerGuard = CommandLine.args.headerGuardPrefix + theClass.name.ToUpper() + "_HPP";
            header.Add("#ifndef " + headerGuard);
            header.Add("#define " + headerGuard);
            header.Add("");
            header.AddRange(theClass.headerDependencies.Select(dependency => String.Format("#include \"{0}.hpp\"", dependency.name)));
            header.AddRange(theClass.unknownDependencies.Select(dependency => String.Format("#include \"unknown_{0}.hpp\"", dependency.type)));

            if (theClass.headerDependencies.Count > 0 || theClass.unknownDependencies.Count > 0)
            {
                header.Add("");
            }

            header.Add("namespace " + CommandLine.args.libNamespace + " {");
            header.Add("");

            if (theClass.sourceDependencies.Count > 0)
            {
                header.Add("// Forward class declarations (defined in the source file)");
                header.AddRange(theClass.sourceDependencies.Select(dependency => String.Format("class {0};", dependency.name)));
                header.Add("");
            }

            header.AddRange(theClass.asClassHeader());
            header.Add("");
            header.Add("}");
            header.Add("");
            header.Add("#endif // " + headerGuard);
        }
    }
}