using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolParser;

namespace SymbolParserTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void FreeStandingFunction()
        {
            ParsedLine line = new ParsedLine("unsigned long long int testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == ParsedClass.FREE_STANDING_CLASS_NAME &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void FreeStandingFunctionParams()
        {
            ParsedLine line = new ParsedLine("unsigned long long int testFunction(unsigned char*** const, const bool, UserType&) 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            bool names = func.parentClass.name == ParsedClass.FREE_STANDING_CLASS_NAME &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 3, "Param count failed to pass");

            bool param1 = func.parameters[0].isBaseType &&
                          func.parameters[0].baseType.Value == BuiltInCppTypes.UNSIGNED_CHAR &&
                          func.parameters[0].type == "unsigned char" &&
                          !func.parameters[0].isConst &&
                          func.parameters[0].isPointer &&
                          func.parameters[0].pointerDepth == 3 &&
                          func.parameters[0].isConstPointer &&
                          !func.parameters[0].isReference;

            if (!param1)
            {
                Assert.Fail("Param1 failed to pass");
            }

            bool param2 = func.parameters[1].isBaseType &&
                          func.parameters[1].baseType.Value == BuiltInCppTypes.BOOL &&
                          func.parameters[1].type == "bool" &&
                          func.parameters[1].isConst &&
                          !func.parameters[1].isPointer &&
                          !func.parameters[1].isConstPointer &&
                          !func.parameters[1].isReference;

            if (!param2)
            {
                Assert.Fail("Param2 failed to pass");
            }

            bool param3 = !func.parameters[2].isBaseType &&
                          func.parameters[2].type == "UserType" &&
                          !func.parameters[2].isConst &&
                          !func.parameters[2].isPointer &&
                          !func.parameters[2].isConstPointer &&
                          func.parameters[2].isReference;

            if (!param3)
            {
                Assert.Fail("Param3 failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void FreeStandingFunctionNoAddr()
        {
            ParsedLine line = new ParsedLine("unsigned long long int testFunction()");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == ParsedClass.FREE_STANDING_CLASS_NAME &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void FreeStandingFunctionCallConv()
        {
            ParsedLine line = new ParsedLine("unsigned long long int __cdecl testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              func.callingConvention.HasValue &&
                              func.callingConvention.Value == FuncCallingConvention.CDECL;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == ParsedClass.FREE_STANDING_CLASS_NAME &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void StaticFreeStandingFunction()
        {
            ParsedLine line = new ParsedLine("static unsigned long long int testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == ParsedClass.FREE_STANDING_CLASS_NAME &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void StaticFreeStandingFunctionCallConv()
        {
            ParsedLine line = new ParsedLine("static unsigned long long int __cdecl testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              func.callingConvention.HasValue &&
                              func.callingConvention.Value == FuncCallingConvention.CDECL;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == ParsedClass.FREE_STANDING_CLASS_NAME &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void NoReturnTypeFreeStandingFunction()
        {
            ParsedLine line = new ParsedLine("testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            Assert.IsTrue(func.returnType == null, "Return type failed to pass.");
            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == ParsedClass.FREE_STANDING_CLASS_NAME &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void ClassFunction()
        {
            ParsedLine line = new ParsedLine("unsigned long long int testClass::testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void ClassFunctionParams()
        {
            ParsedLine line = new ParsedLine("unsigned long long int testClass::testFunction(unsigned char*** const, const bool, UserType&) 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 3, "Param count failed to pass");

            bool param1 = func.parameters[0].isBaseType &&
                          func.parameters[0].baseType.Value == BuiltInCppTypes.UNSIGNED_CHAR &&
                          func.parameters[0].type == "unsigned char" &&
                          !func.parameters[0].isConst &&
                          func.parameters[0].isPointer &&
                          func.parameters[0].pointerDepth == 3 &&
                          func.parameters[0].isConstPointer &&
                          !func.parameters[0].isReference;

            if (!param1)
            {
                Assert.Fail("Param1 failed to pass");
            }

            bool param2 = func.parameters[1].isBaseType &&
                          func.parameters[1].baseType.Value == BuiltInCppTypes.BOOL &&
                          func.parameters[1].type == "bool" &&
                          func.parameters[1].isConst &&
                          !func.parameters[1].isPointer &&
                          !func.parameters[1].isConstPointer &&
                          !func.parameters[1].isReference;

            if (!param2)
            {
                Assert.Fail("Param2 failed to pass");
            }

            bool param3 = !func.parameters[2].isBaseType &&
                          func.parameters[2].type == "UserType" &&
                          !func.parameters[2].isConst &&
                          !func.parameters[2].isPointer &&
                          !func.parameters[2].isConstPointer &&
                          func.parameters[2].isReference;

            if (!param3)
            {
                Assert.Fail("Param3 failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void ClassFunctionNoAddr()
        {
            ParsedLine line = new ParsedLine("unsigned long long int testClass::testFunction()");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void ClassFunctionConst()
        {
            ParsedLine line = new ParsedLine("unsigned long long int testClass::testFunction() const 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void ClassFunctionCallConv()
        {
            ParsedLine line = new ParsedLine("unsigned long long int __cdecl testClass::testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              func.callingConvention.HasValue &&
                              func.callingConvention.Value == FuncCallingConvention.CDECL;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                        func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void ClassFunctionConstructor()
        {
            ParsedLine line = new ParsedLine("testClass::testClass() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            Assert.IsTrue(func.returnType == null, "Return type failed to pass.");
            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testClass";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                        func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void ClassFunctionDestructor()
        {
            ParsedLine line = new ParsedLine("testClass::~testClass() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            Assert.IsTrue(func.returnType == null, "Return type failed to pass.");
            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "~testClass";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                        func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void VirtualClassFunction()
        {
            ParsedLine line = new ParsedLine("virtual unsigned long long int testClass::testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void VirtualClassFunctionCallConv()
        {
            ParsedLine line = new ParsedLine("virtual unsigned long long int __cdecl testClass::testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              !func.isStatic &&
                              func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              func.callingConvention.HasValue &&
                              func.callingConvention.Value == FuncCallingConvention.CDECL;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void StaticClassFunction()
        {
            ParsedLine line = new ParsedLine("static unsigned long long int testClass::testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void StaticClassFunctionCallConv()
        {
            ParsedLine line = new ParsedLine("static unsigned long long int __cdecl testClass::testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              func.callingConvention.HasValue &&
                              func.callingConvention.Value == FuncCallingConvention.CDECL;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void VirtualStaticClassFunction()
        {
            ParsedLine line = new ParsedLine("virtual static unsigned long long int testClass::testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              func.isStatic &&
                              func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void VirtualStaticClassFunctionCallConv()
        {
            ParsedLine line = new ParsedLine("virtual static unsigned long long int __cdecl testClass::testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = !func.accessLevel.HasValue &&
                              func.isStatic &&
                              func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              func.callingConvention.HasValue &&
                              func.callingConvention.Value == FuncCallingConvention.CDECL;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void AccessClassFunction()
        {
            ParsedLine line = new ParsedLine("public: unsigned long long int testClass::testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = func.accessLevel.HasValue &&
                              func.accessLevel.Value == FuncAccessLevel.PUBLIC &&
                              !func.isStatic &&
                              !func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              !func.callingConvention.HasValue;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void VirtualStaticAccessClassFunctionCallConv()
        {
            ParsedLine line = new ParsedLine("private: virtual static unsigned long long int __cdecl testClass::testFunction() 0xFFFFFFFF");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);

            bool decorators = func.accessLevel.HasValue &&
                              func.accessLevel.Value == FuncAccessLevel.PRIVATE &&
                              func.isStatic &&
                              func.isVirtual &&
                              !func.isConstructor &&
                              !func.isDestructor &&
                              func.callingConvention.HasValue &&
                              func.callingConvention.Value == FuncCallingConvention.CDECL;

            if (!decorators)
            {
                Assert.Fail("Decorators failed to pass");
            }

            bool returnType = func.returnType.isBaseType &&
                              func.returnType.baseType.Value == BuiltInCppTypes.UNSIGNED_LONG_LONG_INT &&
                              func.returnType.type == "unsigned long long int" &&
                              !func.returnType.isConst &&
                              !func.returnType.isPointer &&
                              !func.returnType.isConstPointer &&
                              !func.returnType.isReference;

            if (!returnType)
            {
                Assert.Fail("Return type failed to pass");
            }

            Assert.IsTrue(func.parameters.Count == 0, "Param count failed to pass");

            bool names = func.parentClass.name == "testClass" &&
                         func.name == "testFunction";

            if (!names)
            {
                Assert.Fail("Names failed to pass");
            }

            bool end = !func.isConst &&
                       func.address == 0xFFFFFFFF;

            if (!end)
            {
                Assert.Fail("End failed to pass");
            }
        }

        [TestMethod]
        public void templateReturnType()
        {
            ParsedLine line = new ParsedLine("template<templateInner> testFunc()");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);
            Assert.IsTrue(func.returnType.type == SymbolParser.SymbolParser.handleTemplatedName("template<templateInner>"), "Return type was invalid.");
        }

        [TestMethod]
        public void templateParamType()
        {
            ParsedLine line = new ParsedLine("testFunc(template<templateInner>)");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);
            Assert.IsTrue(func.parameters[0].type == SymbolParser.SymbolParser.handleTemplatedName("template<templateInner>"), "Params type was invalid.");
        }

        [TestMethod]
        public void templateClassType()
        {
            ParsedLine line = new ParsedLine("template<templateInner>::testFunc()");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);
            Assert.IsTrue(func.parentClass.name == SymbolParser.SymbolParser.handleTemplatedName("template<templateInner>"), "Class name was invalid.");
        }

        [TestMethod]
        public void templatedClassNameSpace()
        {
            ParsedLine line = new ParsedLine("template<unsigned long long int>::testFunc()");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);
            Assert.IsTrue(func.parentClass.name == SymbolParser.SymbolParser.handleTemplatedName("template<unsignedlonglongint>"), "Return type was invalid.");
        }

        [TestMethod]
        public void templateNestedBadThings()
        {
            ParsedLine line = new ParsedLine("template<template<template<unsigned long long int>>> template<template<template<unsigned long long int>>>::testFunc(template<template<template<unsigned long long int>>>, template<template<template<unsigned long long int>>>***, template<template<template<unsigned long long int>>>&)");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);
        }

        [TestMethod]
        public void Types()
        {
            ParsedLine line = new ParsedLine("CExoLinkedList<unsigned long>::AddTail(unsigned long *) 0x80B1670");
            ParsedClass theClass = new ParsedClass(line);
            ParsedFunction func = new ParsedFunction(line, theClass);
        }
    }
}