// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    public interface IOperator
    {
        int Precedence { get; }
        void Invoke(List<object> list);
    }
}
