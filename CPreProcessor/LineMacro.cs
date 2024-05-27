// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    internal class LineMacro : IDefinedMacro
    {
        readonly Processor cpp;

        internal LineMacro(Processor cpp)
        {
            this.cpp = cpp;
        }

        public string Name => "__LINE__";

        public List<string> Parameters => null;

        public char[] Value => cpp.line.ToString().ToCharArray();
    }
}
