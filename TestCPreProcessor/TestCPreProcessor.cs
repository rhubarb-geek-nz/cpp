// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

namespace RhubarbGeekNz.CPreProcessor
{
    internal class ListAppender : IStringPipeline
    {
        readonly List<string> list;
        internal Boolean disposed = false;

        internal ListAppender(List<string> list)
        {
            this.list = list;
        }

        public void Dispose()
        {
            if (disposed) throw new IOException("already closed");

            disposed = true;
        }

        public void Write(string s)
        {
            list.Add(s);
        }
    }

    class CPreProcessorTest : Processor
    {
        internal CPreProcessorTest(IProcessorBuilder builder) : base(builder)
        {
        }

        internal long EvaluateConditionInBase(string s)
        {
            return base.EvaluateCondition(s);
        }

        internal static IProcessorBuilder CreateTestBuilder()
        {
            return new ProcessorBuilder((t) => new CPreProcessorTest(t));
        }
    }

    [TestClass]
    public class TestCPreProcessor
    {
        readonly bool replaceNewLine = Environment.NewLine.Length > 1;
        readonly Tokenizer tokenizer = new Tokenizer(null);

        public TestCPreProcessor()
        {
        }

        [TestMethod]
        public void TestAssemblyVersion()
        {
            Assert.AreEqual("1.0.0.0", typeof(Processor).Assembly.GetName().Version.ToString());
        }

        [TestMethod]
        public void TestABC()
        {
            RunAndAssert("""
                        a
                        b
                        c
                        """,
                        """
                        a
                        b
                        c
                        """);
        }

        [TestMethod]
        public void TestTriGraphs()
        {
            RunAndAssert("""
                        a??=A
                        b??/B
                        c??'C
                        d??(D
                        e??)E
                        f??!F
                        g??<G
                        h??>H
                        i??-I
                        """,
                        """
                        a#A
                        b\B
                        c^C
                        d[D
                        e]E
                        f|F
                        g{G
                        h}H
                        i~I
                        """);
        }

        [TestMethod]
        public void TestLineJoiner()
        {
            RunAndAssert("""
                        A\
                        B
                        C??/
                        D
                        """,
                        """
                        AB
                        CD
                        """);
        }

        [TestMethod]
        public void TestSubstitutionCAT()
        {
            RunAndAssert("""
                        #define HE HI
                        #define LLO _THERE
                        #define HELLO "HI THERE"
                        #define CAT(a,b) a##b
                        #define XCAT(a,b) CAT(a,b)
                        #define CALL(fn) fn(HE,LLO)
                        CAT(HE, LLO) // "HI THERE", because concatenation occurs before normal expansion
                        """,
                        """
                        
                        
                        
                        
                        
                        
                        "HI THERE" 
                        """);
        }

        [TestMethod]
        public void TestSubstitutionXCAT()
        {
            RunAndAssert("""
                        #define HE HI
                        #define LLO _THERE
                        #define HELLO "HI THERE"
                        #define CAT(a,b) a##b
                        #define XCAT(a,b) CAT(a,b)
                        #define CALL(fn) fn(HE,LLO)
                        XCAT(HE, LLO) // HI_THERE, because the tokens originating from parameters ("HE" and "LLO") are expanded first
                        """,
                        """
                        
                        
                        
                        
                        
                        
                        HI_THERE 
                        """);
        }
        [TestMethod]
        public void TestSubstitutionCALL()
        {
            RunAndAssert("""
                        #define HE HI
                        #define LLO _THERE
                        #define HELLO "HI THERE"
                        #define CAT(a,b) a##b
                        #define XCAT(a,b) CAT(a,b)
                        #define CALL(fn) fn(HE,LLO)
                        CALL(CAT) // "HI THERE", because this evaluates to CAT(a,b)
                        """,
                        """
                        
                        
                        
                        
                        
                        
                        "HI THERE" 
                        """);
        }

        [TestMethod]
        public void TestSubstitutionAardVark()
        {
            RunAndAssert("""
                        #define HE HI
                        #define LLO _THERE
                        #define HELLO "HI THERE"
                        #define CAT(a,b) a##b
                        #define XCAT(a,b) CAT(a,b)
                        #define CALL(fn) fn(HE,LLO)
                        CAT(aard,vark) // aardvark
                        """,
                        """
                        
                        
                        
                        
                        
                        
                        aardvark 
                        """);
        }

        [TestMethod]
        public void TestLineContinuationBackSlash()
        {
            RunAndAssert("""
                        FOO\
                        BAR
                        """,
                        """
                        FOOBAR
                        """);
        }

        [TestMethod]
        public void TestLineContinuationTrigraph()
        {
            RunAndAssert("""
                        FOO??/
                        BAR
                        """,
                        """
                        FOOBAR
                        """);
        }


        [TestMethod]
        public void TestStringify()
        {
            RunAndAssert("""
                        #define Q1( x )    #x

                        Q1( a )
                        Q1( (b) )
                        Q1( ( foo , bar ) )
                        Q1( " foo	bar\t" ) // contains an actual tab character
                        """,
                        """
                        
                        
                        "a"
                        "(b)"
                        "( foo , bar )"
                        "\" foo\tbar\\t\"" 
                        """);
        }

        [TestMethod]
        public void TestMacros()
        {
            RunAndAssert("""
                           #   define HE HI
                        HE
                        """,
                        """

                        HI
                        """);
        }

        [TestMethod]
        public void TestFileMacros()
        {
            RunAndAssert("""
                        __FILE__
                        __LINE__
                        __LINE__
                        """,
                        """
                        "<stdin>"
                        2
                        3
                        """);
        }

        [TestMethod]
        public void TestCppComment()
        {
            RunAndAssert("""
                        FOO // this is a comment
                        """,
                        """
                        FOO 
                        """);
        }

        [TestMethod]
        public void TestSingleLineComment()
        {
            RunAndAssert("""
                        FOO /* this is a comment */ BAR
                        """,
                        """
                        FOO  BAR
                        """);
        }

        [TestMethod]
        public void TestNestedExpansion()
        {
            RunAndAssert("""
                        #define MYVAL  4
                        #define MYFUNC  A + MYVAL + B
                        #define MYLINE C + MYFUNC + D
                        MYLINE
                        #undef MYVAL
                        MYLINE
                        #define MYVAL  5
                        MYLINE
                        """,
                        """
                        


                        C + A + 4 + B + D

                        C + A + MYVAL + B + D

                        C + A + 5 + B + D
                        """);
        }

        [TestMethod]
        public void TestMultiLineComment()
        {
            RunAndAssert("""
                        FOO /* this is
                        a comment */ BAR
                        """,
                        """
                        FOO 
                         BAR
                        """);
        }

        [TestMethod]
        public void TestIfElseEndIf()
        {
            RunAndAssert("""
                        #if 1
                        foo
                        #else
                        bar
                        #endif
                        """,
                        """

                        foo



                        """);
        }

        [TestMethod]
        public void TestINestedfElseEndIf()
        {
            RunAndAssert("""
                        #if 1
                        foo 1
                        #   if 1
                        foo 2
                        #   else
                        bar 2
                        #   endif
                        bar 3
                        #else
                        bar 4
                        #endif
                        """,
                        """

                        foo 1

                        foo 2



                        bar 3



                        """);
        }

        [TestMethod]
        public void TestIfDef()
        {
            RunAndAssert("""
                        #ifdef FOO
                        bar 1
                        #endif
                        #ifndef FOO
                        bar 2
                        #endif
                        #define FOO 1
                        #ifdef FOO
                        bar 3
                        #endif
                        #undef FOO
                        #ifdef FOO
                        bar 4
                        #endif
                        """,
                        """
                        
                        
                        
                        
                        bar 2
                        
                        
                        
                        bar 3
                        




                        """);
        }

        [TestMethod]
        public void TestDeepDefines()
        {
            RunAndAssert("""
                        #define A 1
                        #define B 2
                        #define C 3
                        #define D 4
                        #define AB A B
                        #define CD C D
                        #define ABCD AB CD
                        __FILE__ ABCD __LINE__
                        """,
                        """
                        


                        
                        
                        

                        "<stdin>" 1 2 3 4 8
                        """);
        }

        [TestMethod]
        public void TestDispose()
        {
            var appender = new ListAppender(new List<string>());

            Assert.IsFalse(appender.disposed);

            var builder = Processor.
                CreateBuilder().
                UseOutputWriter(appender).
                UseErrorHandler((c, s) => { return true; }).
                UseWarningHandler((c, s) => { return true; });

            using (var cpp = builder.Build())
            {
                Assert.IsFalse(appender.disposed);
            }

            Assert.IsTrue(appender.disposed);
        }

        void RunAndAssert(string input, string output)
        {
            List<string> strings = new List<string>();

            var builder = Processor.
                CreateBuilder().
                UseBlankLines(true).
                UseTriGraphs(true).
                UseOutputWriter(new ListAppender(strings)).
                UseErrorHandler((c, s) => { return true; }).
                UseWarningHandler((c, s) => { return true; });

            using (var cpp = builder.Build())
            {
                foreach (string str in (replaceNewLine ? input.Replace(Environment.NewLine, "\n") : input).Split('\n'))
                {
                    cpp.Write(str);
                }
            }

            string result = String.Join(Environment.NewLine, strings);
            Assert.AreEqual(output, result);
        }


        [TestMethod]
        public void TestEmptyBrackets()
        {
            TestExpression(2, "()    ");
        }

        [TestMethod]
        public void TestNestedEmptyBrackets()
        {
            TestExpression(7, "((),())    ");
        }

        [TestMethod]
        public void TestMultipleBrackets()
        {
            TestExpression(17, "((a,b,c),(d,e,f))    ");
        }

        [TestMethod]
        public void TestNumber()
        {
            TestExpression(3, "123,456    ");

        }

        [TestMethod]
        public void TestSingleBrackets()
        {
            TestExpression(7, "(1,234)    ");
        }

        [TestMethod]
        public void TestSimpleToken()
        {
            TestExpression(3, "abc    ");
        }

        [TestMethod]
        public void TestUnderscoreToken()
        {
            TestExpression(7, "__abc__    ");
        }

        [TestMethod]
        public void TestSpaces()
        {
            TestExpression(3, "   abcd    ");
        }
        [TestMethod]
        public void TestAllWhitespace()
        {
            TestExpression(3, " \t\nabcd    ");
        }
        [TestMethod]
        public void TestStringWithSpace()
        {
            TestExpression(3, "\' \'    ");
        }

        [TestMethod]
        public void TestStringWithEscape()
        {
            TestExpression(4, "\"\\n\"    ");
        }

        [TestMethod]
        public void TestDoubleHash()
        {
            TestExpression(2, "##    ");
        }

        [TestMethod]
        public void TestTripleHash()
        {
            TestExpression(2, "###    ");
        }

        void TestExpression(int length, string line)
        {
            Assert.AreEqual(length, tokenizer.ExpressionLength(line.ToCharArray(), 0, line.Length));
        }

        [TestMethod]
        public void TestUnbalancedEndif()
        {
            bool wasCaught = false;
            try
            {
                RunAndAssert("#endif", String.Empty);
            }
            catch (CPreProcessorException ex)
            {
                wasCaught = ex.Message.EndsWith("#endif");
            }
            Assert.IsTrue(wasCaught);
        }

        [TestMethod]
        public void TestPragma()
        {
            RunAndAssert("#pragma", "#pragma");
        }

        [TestMethod]
        public void TestUnknownDirective()
        {
            RunAndAssert("#foo", "#foo");
        }

        [TestMethod]
        public void TestUnclosedIf()
        {
            bool wasCaught = false;
            try
            {
                RunAndAssert("#if 1", String.Empty);
            }
            catch (CPreProcessorException ex)
            {
                wasCaught = ex.Message.EndsWith("#endif");
            }
            Assert.IsTrue(wasCaught);
        }

        [TestMethod]
        public void TestElseWithNoIf()
        {
            bool wasCaught = false;
            try
            {
                RunAndAssert("#else", String.Empty);
            }
            catch (CPreProcessorException ex)
            {
                wasCaught = ex.Message.EndsWith("#if");
            }
            Assert.IsTrue(wasCaught);
        }

        [TestMethod]
        public void TestElIfWithNoIf()
        {
            bool wasCaught = false;
            try
            {
                RunAndAssert("#elif", String.Empty);
            }
            catch (CPreProcessorException ex)
            {
                wasCaught = ex.Message.EndsWith("#if");
            }
            Assert.IsTrue(wasCaught);
        }

        [TestMethod]
        public void TestTokenLength()
        {
            Assert.IsTrue(tokenizer.IsIdentifier("    foo     ".ToCharArray(), 4, 10, out int len1));
            Assert.AreEqual(3, len1);
            Assert.IsFalse(tokenizer.IsIdentifier("   foo     ".ToCharArray(), 2, 10, out int len2));
            Assert.AreEqual(0, len2);
        }

        [TestMethod]
        public void TestTokenPaste()
        {
            Assert.IsTrue(tokenizer.IsTokenPaste("foo ## bar ".ToCharArray(), 3, 10, out int len1, out int len3));
            Assert.AreEqual(4, (int)len1);
            Assert.AreEqual(3, (int)len3);
            Assert.IsFalse(tokenizer.IsTokenPaste("foo # bar ".ToCharArray(), 4, 10, out int len2, out int len4));
            Assert.AreEqual(0, (int)len2);
            Assert.AreEqual(0, (int)len4);
        }

        [TestMethod]
        public void TestStringCode()
        {
            RunAndAssert("""
                        #define str(s) #s

                        str(p = "foo\n";) // outputs "p = \"foo\\n\";"
                        str(\n)           // outputs "\n"
                        """,
                        """
                        

                        "p = \"foo\\n\";" 
                        "\\n"           
                        """);
        }


        [TestMethod]
        public void TestStringToken4()
        {
            RunAndAssert("""
                        #define xstr(s) str(s)
                        #define str(s) #s
                        #define foo 4

                        xstr (foo) // outputs "4"
                        """,
                        """
                        



                        "4" 
                        """);

        }

        [TestMethod]
        public void TestStringTokenFoo()
        {
            RunAndAssert("""
                        #define xstr(s) str(s)
                        #define str(s) #s
                        #define foo 4

                        str (foo)  // outputs "foo"
                        """,
                        """
                        



                        "foo"  
                        """);

        }

        [TestMethod]
        public void TestBarBaz1()
        {
            RunAndAssert("""
                        #define foo()bar
                        foo()baz
                        """,
                        """

                        barbaz
                        """);

        }

        [TestMethod]
        public void TestBarBaz2()
        {
            RunAndAssert("""
                        #define foo() bar
                        foo()baz
                        """,
                        """

                        barbaz
                        """);

        }

        [TestMethod]
        public void TestCondition1()
        {
            RunAndAssert("""
                        #if 1
                        true 1
                        #endif
                        """,
                        """

                        true 1

                        """);

        }

        [TestMethod]
        public void TestCondition2()
        {
            RunAndAssert("""
                        #if 0
                        false 1
                        #endif
                        """,
                        """

                        

                        """);

        }

        [TestMethod]
        public void TestCondition3()
        {
            RunAndAssert("""
                        #if 0
                        false 1
                        #else
                        true 2
                        #endif
                        """,
                        """



                        true 2

                        """);

        }


        [TestMethod]
        public void TestCondition4()
        {
            RunAndAssert("""
                        #if 1
                        true 1
                        #else
                        false 2
                        #endif
                        """,
                        """

                        true 1

                        

                        """);

        }

        [TestMethod]
        public void TestCondition5()
        {
            RunAndAssert("""
                        #if 1
                        true 1
                        #elif this should be ignore
                        ignored 2
                        #else
                        false 3
                        #endif
                        """,
                        """

                        true 1



                        

                        """);

        }

        [TestMethod]
        public void TestCondition6()
        {
            RunAndAssert("""
                        #if 0
                        false 1
                        #elif 1
                        true 2
                        #else
                        false 3
                        #endif
                        """,
                        """

                        

                        true 2


                        
                        """);

        }

        [TestMethod]
        public void TestCondition7()
        {
            RunAndAssert("""
                        #if 0
                        false 1
                        #elif 0
                        false 2
                        #elif 1
                        true 3
                        #elif 1
                        true 4
                        #else
                        false 5
                        #endif
                        """,
                        """

                        



                        true 3




                        
                        """);

        }

        void TestEvaluate(long expected, string exp)
        {
            List<string> list = new List<string>();
            var builder = CPreProcessorTest.
                CreateTestBuilder().
                UseOutputWriter(new ListAppender(list)).
                UseErrorHandler((c, s) => { return true; }).
                UseWarningHandler((c, s) => { return true; });
            CPreProcessorTest cpp = (CPreProcessorTest)builder.Build();
            cpp.Write("#define FORTYTWO 42");
            long val = cpp.EvaluateConditionInBase(exp);
            Assert.AreEqual(expected, val, exp);
        }

        void TestEvaluateException(string exp)
        {
            bool wasCaught = false;
            List<string> list = new List<string>();
            var builder = CPreProcessorTest.
                CreateTestBuilder().
                UseOutputWriter(new ListAppender(list)).
                UseErrorHandler((c, s) => { return true; }).
                UseWarningHandler((c, s) => { return true; });
            CPreProcessorTest cpp = (CPreProcessorTest)builder.Build();
            cpp.Write("#define FORTYTWO 42");
            try
            {
                cpp.EvaluateConditionInBase(exp);
            }
            catch (CPreProcessorException)
            {
                wasCaught = true;
            }
            Assert.IsTrue(wasCaught);
        }

        [TestMethod]
        public void TestEvaluations()
        {
            TestEvaluate(0, "0");
            TestEvaluate(1, "1");
            TestEvaluate(2, "2");
            TestEvaluate(42, "42");
            TestEvaluate(2, "(2)");
            TestEvaluate(3, " ( 3 ) ");
            TestEvaluate(1, " defined __LINE__ ");
            TestEvaluate(0, " defined FOOBAR ");
            TestEvaluate(0, " defined(FOOBAR) ");
            TestEvaluate(0, " defined ( FOOBAR ) ");
            TestEvaluate(1, " defined ( __LINE__ ) ");
            TestEvaluate(1, " __LINE__ ");
            TestEvaluate(1, " defined ( FORTYTWO ) ");
            TestEvaluate(1, " defined FORTYTWO ");
            TestEvaluate(42, "FORTYTWO ");
        }

        [TestMethod]
        public void TestEvaluationsUnary()
        {
            TestEvaluate(1, "!0");
            TestEvaluate(0, "!1");
            TestEvaluate(0, "-0");
            TestEvaluate(-1, "-1");
            TestEvaluate(-42, "-FORTYTWO");
            TestEvaluate(42, "--42");
            TestEvaluate(-42, "---42");
            TestEvaluate(-42, "---FORTYTWO");
        }

        [TestMethod]
        public void TestEvaluationBinary()
        {
            TestEvaluate(7, "4+3");
            TestEvaluate(7, "5|3");
            TestEvaluate(7, "10-3");
            TestEvaluate(7, "FORTYTWO/6");

            TestEvaluate(1, "4 > 3");
            TestEvaluate(0, "4 > 5");

            TestEvaluate(1, "4 >= 3");
            TestEvaluate(1, "4 >= 4");
            TestEvaluate(0, "4 >= 5");

            TestEvaluate(1, "3 < 4");
            TestEvaluate(0, "5 < 4");

            TestEvaluate(1, "3 <= 4");
            TestEvaluate(1, "4 <= 4");
            TestEvaluate(0, "5 <= 4");

            TestEvaluate(1, "5 == 5");
            TestEvaluate(0, "4 == 5");

            TestEvaluate(0, "5 != 5");
            TestEvaluate(1, "4 != 5");
        }

        [TestMethod]
        public void TestEvaluationNumbers()
        {
            TestEvaluate(42, "0x2a");
            TestEvaluate(42, "0x2A");
            TestEvaluate(42, "42");
            TestEvaluate(42, "FORTYTWO");
            TestEvaluate(8, "010");

        }

        [TestMethod]
        public void TestEvaluationBrackets()
        {
            TestEvaluate(42, "2+(20 * 2)");
            TestEvaluate(42, "6*(6+1)");
            TestEvaluate(42, "(7*3)*2");
            TestEvaluate(42, "-(7*-3)*2");
        }

        [TestMethod]
        public void TestEvaluatioPrecedence()
        {
            TestEvaluate(5 * 2 + 4 * 3, "5*2+4*3");
            TestEvaluate(9 + 7 * 8, "9+7*8");
        }

        [TestMethod]
        public void TestEvaluatioIfElse()
        {
            TestEvaluate(4 > 3 ? 4 : 3, "4 > 3  ? 4 : 3");
            TestEvaluate(9 < 10 ? 9 : 10, "9 < 10 ? 9 : 10");
        }

        [TestMethod]
        public void TestEvaluatioExceptions()
        {
            TestEvaluateException("4 >");
            TestEvaluateException("0 ?");
            TestEvaluateException("0 :");
            TestEvaluateException("? 0");
            TestEvaluateException("+");
            TestEvaluateException("==");
            TestEvaluateException("0 ==");
            TestEvaluateException("== 0");
        }

        [TestMethod]
        public void TestMultiStringEndOfLine()
        {
            string s = """
                        foo
                        bar
                        """;
            string[] lines = s.Split(Environment.NewLine);
            Assert.AreEqual(2, lines.Length);
            Assert.AreEqual(lines[0], "foo");
            Assert.AreEqual(lines[1], "bar");
        }

        [TestMethod]
        public void TestIncludeQuotes()
        {
            List<string> list = new List<string>();
            var builder = CPreProcessorTest.
                CreateTestBuilder().
                UseLineNumbers(true).
                UseOutputWriter(new ListAppender(list)).
                UseErrorHandler((c, s) => { return true; }).
                UseIncludeDirectory(Environment.CurrentDirectory).
                UseWarningHandler((c, s) => { return true; });
            using (CPreProcessorTest cpp = (CPreProcessorTest)builder.Build())
            {
                cpp.Write("#include \"test.h\"");
            }
            string result = String.Join(Environment.NewLine, list.ToArray());

            Assert.AreEqual(
                """
                # 1 "<stdin>"
                # 1 "test.h"
                int ANSWER = 42;
                # 2 "<stdin>"
                """,
                result);
        }

        [TestMethod]
        public void TestIncludeAngular()
        {
            string dir = Environment.CurrentDirectory;
            List<string> list = new List<string>();
            var builder = CPreProcessorTest.
                CreateTestBuilder().
                UseLineNumbers(true).
                UseOutputWriter(new ListAppender(list)).
                UseErrorHandler((c, s) => { return true; }).
                UseIncludePath(dir).
                UseWarningHandler((c, s) => { return true; });
            using (CPreProcessorTest cpp = (CPreProcessorTest)builder.Build())
            {
                cpp.Write("#include <test.h>");
            }
            string result = String.Join(Environment.NewLine, list.ToArray());

            result = result.Replace(dir + Path.DirectorySeparatorChar, "./");

            Assert.AreEqual(
                """
                # 1 "<stdin>"
                # 1 "./test.h"
                int ANSWER = 42;
                # 2 "<stdin>"
                """,
                result);
        }

        [TestMethod]
        public void TestNoInclude()
        {
            RunAndAssert("""
                        #if 0
                        #include "test.h"
                        #endif
                        """,
                        """


                        
                        """);
        }

        [TestMethod]
        public void TestNoDefine()
        {
            RunAndAssert("""
                        #if 0
                        #define FOO 1
                        #endif
                        #if FOO
                        #error FOO 2
                        #endif
                        #ifdef FOO
                        #error FOO 3
                        #endif
                        """,
                        """


                        
                        
                        
                        
                        
                        
                        
                        """);
        }

        [TestMethod]
        public void TestNoWarning()
        {
            RunAndAssert("""
                        #if 0
                        #warning FOO
                        #endif
                        """,
                        """
                        
                        

                        """);
        }

        [TestMethod]
        public void TestNoError()
        {
            RunAndAssert("""
                        #if 0
                        #error FOO
                        #endif
                        """,
                        """
                        
                        

                        """);
        }
    }
}
