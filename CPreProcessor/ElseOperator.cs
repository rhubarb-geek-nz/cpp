// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    internal class ElseOperator : IOperator
    {
        internal long Value;
        public int Precedence => 20;

        public void Invoke(List<object> list)
        {
            int index = list.IndexOf(this) ;
            Value = (long)list[index + 1];
            list.RemoveRange(index, 2);

            while (index-- > 0)
            {
                object o = list[index];

                if (o is IfOperator)
                {
                    ((IfOperator)o).ElseValue = this;
                    break;
                }
            }
        }
    }
}
