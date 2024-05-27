// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    internal class IfOperator : IOperator
    {
        internal ElseOperator ElseValue;
        public int Precedence => 21;

        public void Invoke(List<object> list)
        {
            int index = list.IndexOf(this) - 1;
            list[index] = 0!=(long)list[index] ? (long)list[index + 2] : ElseValue.Value;
            list.RemoveRange(index + 1, 2);
        }
    }
}
