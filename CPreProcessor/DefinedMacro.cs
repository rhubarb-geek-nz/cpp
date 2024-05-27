// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    internal class DefinedMacro : IDefinedMacro
    {
        public string Name { get; }
        public List<string> Parameters { get; }
        public char [] Value { get; }

        internal DefinedMacro(string name, List<string> parameters, char [] value)
        {
            Name = name;
            Parameters = parameters;
            Value = value;
        }
    }
}
