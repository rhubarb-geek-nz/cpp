// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    internal class FileMacro : IDefinedMacro
    {
        readonly Processor cpp;

        internal FileMacro(Processor cpp)
        {
            this.cpp = cpp;
        }

        public string Name => "__FILE__";

        public List<string> Parameters => null;

        public char[] Value => cpp.file.ToCharArray();
    }
}
