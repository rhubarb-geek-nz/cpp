// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace RhubarbGeekNz.CPreProcessor.PowerShell
{
    [TestClass]
    public class UnitTests
    {
        readonly InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
        public UnitTests()
        {
            foreach (Type t in new Type[] {
                typeof(InvokeCPreProcessor)
            })
            {
                CmdletAttribute? ca = t.GetCustomAttribute<CmdletAttribute>();

                if (ca == null) throw new NullReferenceException();

                initialSessionState.Commands.Add(new SessionStateCmdletEntry($"{ca.VerbName}-{ca.NounName}", t, ca.HelpUri));
            }
        }


        [TestMethod]
        public void TestFoo()
        {
            List<string> result = Invoke("'foo' | Invoke-CPreProcessor");
            Assert.AreEqual("foo", String.Join('\n', result));
        }

        [TestMethod]
        public void TestABC()
        {
            List<string> result = Invoke("('a','b','c') | Invoke-CPreProcessor");
            Assert.AreEqual("a\nb\nc", String.Join('\n', result));
        }

        [TestMethod]
        public void TestDefineOnly()
        {
            List<string> result = Invoke("('#define FOO') | Invoke-CPreProcessor");
            Assert.AreEqual(0,result.Count);
        }

        [TestMethod]
        public void TestCommentOnly()
        {
            List<string> result = Invoke("('// line comment') | Invoke-CPreProcessor");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestPathNotFollowed()
        {
            List<string> result = Invoke("('#if 0','FOO','#endif') | Invoke-CPreProcessor");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestHelloWorld()
        {
            List<string> result = Invoke("""
                @'
                // hello world sample
                #if FOO
                Hello World
                #else
                Something went wrong
                #endif
                '@ | Invoke-CPreProcessor -MacroDefinitions @{ FOO = 1 }
                """);
            Assert.AreEqual("Hello World", String.Join('\n', result));
        }

        [TestMethod]
        public void TestIfElseEndif()
        {
            List<string> result = Invoke("('#if 1','#else','#endif') | Invoke-CPreProcessor");
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void TestABC2()
        {
            List<string> result = Invoke("@\"\na\nb\nc\n\"@\n | Invoke-CPreProcessor");
            Assert.AreEqual("a\nb\nc", String.Join('\n', result));
        }

        [TestMethod]
        public void TestABC3()
        {
            List<string> result = Invoke("""
                @"
                a
                b
                c
                "@ | Invoke-CPreProcessor
                """);
            Assert.AreEqual("a\nb\nc", String.Join('\n', result));
        }

        [TestMethod]
        public void TestInputString()
        {
            List<string> result = Invoke("Invoke-CPreProcessor -InputString 'foo'");
            Assert.AreEqual("foo", String.Join('\n', result));
        }

        [TestMethod]
        public void TestError()
        {
            bool wasCaught = false;
            try
            {
                Invoke("""
                    @"
                    #endif
                    "@ | Invoke-CPreProcessor -ErrorAction 'Stop'
                    """);
            }
            catch (ActionPreferenceStopException ex)
            {
                wasCaught = ex.Message.EndsWith("#endif");
            }
            Assert.IsTrue(wasCaught);
        }

        List<string> Invoke(string script)
        {
            var powerShell = System.Management.Automation.PowerShell.Create(initialSessionState);
            powerShell.AddScript(script.Replace("\r\n", "\n"));
            return powerShell.Invoke().Select(o => o.ToString()).ToList();
        }

        [TestMethod]
        public void TestMacroDefinitions()
        {
            List<string> result = Invoke("Invoke-CPreProcessor -InputString 'FOO' -MacroDefinitions @{ FOO = 42 }");
            Assert.AreEqual("42", String.Join('\n', result));
        }
    }
}
