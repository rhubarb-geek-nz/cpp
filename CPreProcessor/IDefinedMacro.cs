// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    public interface IDefinedMacro
    {
        string Name { get; }
        List<string> Parameters { get; }
        char [] Value { get; }
    }
}
