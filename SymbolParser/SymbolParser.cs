using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// TASK LIST:
// 1. Dehack. DEHACK! SymbolParser front-end is full of full paths. Let's rely on the command line.
// 2. Remove all usages of the preprocessor.
// 2. Improve platform dependant types (__int32). Add flag to indicate platform dependant, and don't apply these during cross referencing.
// 4. Command line params: -parse "rawdumplinux.txt" -crossref "rawdumpwindows.txt" "rawdumpwindows2.txt" -namespaces "NWNXLib" -makeclasses  -makefunctions -classnamespaces "Classes" -functionnamespaces "Functions" -headerguardprefix "NWNX_UNIFIED__" -log "log.txt" -outputdir "output"
// 5. Perhaps improve the implementation of unknown types.

namespace SymbolParser
{
    public class SymbolParser
    {
        public const string IN_DIR = @"D:\Development\\NWNX2-Unified\Scripts";
        public const string OUT_DIR = @"D:\Development\NWNX2-Unified\NWNXLib\Current\GameAPI";
        public const string OUT_CLASS_FOLDER_WINDOWS = @"ClassWrappers\Windows";
        public const string OUT_CLASS_FOLDER_LINUX = @"ClassWrappers\Linux";
        public const string OUT_FUNCTION_FOLDER_WINDOWS = null;
        public const string OUT_FUNCTION_FOLDER_LINUX = null;

        public const string WINDOWS_RAW_FILE_NAME = "rawdumpwindows.txt";
        public const string LINUX_RAW_FILE_NAME = "rawdumplinux.txt";

        public const string WINDOWS_PARSED_FILE_NAME = "parsedwindows.txt";
        public const string LINUX_PARSED_FILE_NAME = "parsedlinux.txt";

#if PARSE_WIN32
        public const string RAW_FILE_NAME = WINDOWS_RAW_FILE_NAME;
        public const string PARSED_FILE_NAME = WINDOWS_PARSED_FILE_NAME;
#else
        public const string RAW_FILE_NAME = LINUX_RAW_FILE_NAME;
        public const string PARSED_FILE_NAME = LINUX_PARSED_FILE_NAME;
#endif

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
            List<string> clean = cleanFile(RAW_FILE_NAME);
            File.WriteAllLines(Path.Combine(IN_DIR, PARSED_FILE_NAME), clean);
            List<ParsedClass> classes = getClasses(getParsedLines(clean));

#if !PARSE_WIN32
            // Since we don't have return types for the Linux build, we should
            // cross reference function names with both the windows symbol files
            // we have.
            List<ParsedClass> winClasses = getClasses(getParsedLines(cleanFile(WINDOWS_RAW_FILE_NAME)));
            crossReference(classes, winClasses);
#endif

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

                matchingBrackets.Add(line.Substring(index, matchingIndex - index));
                index = line.IndexOf('<', matchingIndex + 1);
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

        private List<string> cleanFile(string fileName)
        {
            return cleanRaw(File.ReadAllLines(Path.Combine(IN_DIR, fileName)).ToList());
        }

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

                lock (cleanList)
                {
                    cleanList.Add(line.Replace(")const", ") const"));
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

            // Determine dependencies
            foreach (ParsedClass theClass in parsedClassDict.Values)
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
                        ParsedClass dependencyClass;
                        parsedClassDict.TryGetValue(handleTemplatedName(dependency.type), out dependencyClass);

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

            return parsedClasses;
        }

        private static void dumpStandaloneFiles(List<ParsedClass> classes)
        {
            const string ns1 = "NWNXLib";
            const string ns2 = "Functions";

#if PARSE_WIN32
            string funcDir = OUT_FUNCTION_FOLDER_WINDOWS != null
                                 ? Path.Combine(OUT_DIR, OUT_FUNCTION_FOLDER_WINDOWS)
                                 : OUT_DIR;
#else
            string funcDir = OUT_FUNCTION_FOLDER_LINUX != null
                                 ? Path.Combine(OUT_DIR, OUT_FUNCTION_FOLDER_LINUX)
                                 : OUT_DIR;
#endif

            if (!Directory.Exists(funcDir))
            {
                Directory.CreateDirectory(funcDir);
            }

            var source = new List<string>();
            var header = new List<string>();

#if PARSE_WIN32
            header.Add("#ifndef NWNX_UNIFIED__FUNCTIONS_WINDOWS_HPP");
            header.Add("#define NWNX_UNIFIED__FUNCTIONS_WINDOWS_HPP");
            source.Add("#include \"FunctionsWindows.hpp\"");
#else
            header.Add("#ifndef NWNX_UNIFIED__FUNCTIONS_LINUX_HPP");
            header.Add("#define NWNX_UNIFIED__FUNCTIONS_LINUX_HPP");
            source.Add("#include \"FunctionsLinux.hpp\"");
#endif

            header.Add("");
            header.Add("namespace NWNXLib {");
            header.Add("");
            header.Add("namespace Functions {");
            header.Add("");

            source.Add("");
            source.Add("namespace NWNXLib {");
            source.Add("");
            source.Add("namespace Functions {");
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

#if PARSE_WIN32
            header.Add("#endif // NWNX_UNIFIED__FUNCTIONS_WINDOWS_HPP");
#else
            header.Add("#endif // NWNX_UNIFIED__FUNCTIONS_LINUX_HPP");
#endif

#if PARSE_WIN32
            File.WriteAllLines(Path.Combine(funcDir, "FunctionsWindows.cpp"), source);
            File.WriteAllLines(Path.Combine(funcDir, "FunctionsWindows.hpp"), header);
#else
            File.WriteAllLines(Path.Combine(funcDir, "FunctionsLinux.cpp"), source);
            File.WriteAllLines(Path.Combine(funcDir, "FunctionsLinux.hpp"), header);
#endif
        }

        private void dumpClassFiles(List<ParsedClass> classes)
        {
#if PARSE_WIN32
            string classDir = OUT_CLASS_FOLDER_WINDOWS != null
                                  ? Path.Combine(OUT_DIR, OUT_CLASS_FOLDER_WINDOWS)
                                  : OUT_DIR;
#else
            string classDir = OUT_CLASS_FOLDER_LINUX != null
                                  ? Path.Combine(OUT_DIR, OUT_CLASS_FOLDER_LINUX)
                                  : OUT_DIR;
#endif

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

                string headerGuard = "NWNX_UNIFIED__UNKNOWN_" + unknownType.type.ToUpper() + "_HPP";
                headerFile.Add("#ifndef " + headerGuard);
                headerFile.Add("#define " + headerGuard);
                headerFile.Add("");
                headerFile.Add(String.Format("typedef void* {0};", unknownType.type));
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

            body.AddRange(theClass.asClassSource());
        }

        private static void buildClassHeader(List<string> header, ParsedClass theClass)
        {
            string headerGuard = "NWNX_UNIFIED__" + theClass.name.ToUpper() + "_HPP";
            header.Add("#ifndef " + headerGuard);
            header.Add("#define " + headerGuard);
            header.Add("");
            header.AddRange(theClass.headerDependencies.Select(dependency => String.Format("#include \"{0}.hpp\"", dependency.name)));
            header.AddRange(theClass.unknownDependencies.Select(dependency => String.Format("#include \"unknown_{0}.hpp\"", dependency.type)));

            if (theClass.headerDependencies.Count > 0 || theClass.unknownDependencies.Count > 0)
            {
                header.Add("");
            }

            if (theClass.sourceDependencies.Count > 0)
            {
                header.Add("// Forward class declarations (defined in the source file.).");
                header.AddRange(theClass.sourceDependencies.Select(dependency => String.Format("class {0};", dependency.name)));
                header.Add("");
            }

            header.AddRange(theClass.asClassHeader());
            header.Add("");
            header.Add("#endif // " + headerGuard);
        }
    }
}