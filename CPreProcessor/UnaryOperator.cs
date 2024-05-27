// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    internal class UnaryOperator : IOperator
    {
        private readonly int precedence;
        public int Precedence => precedence;

        private readonly Func<long, long> InvokeOperator;

        public void Invoke(List<object> list)
        {
            int index = list.IndexOf(this);

            while (index >= 0)
            {
                if (list.Count > index)
                {
                    object value = list[index + 1];

                    if (value is IOperator)
                    {
                        ((IOperator)value).Invoke(list);
                    }
                    else
                    {
                        list[index] = InvokeOperator((long)value);
                        list.RemoveAt(index + 1);
                        index = -1;
                    }
                }
                else
                {
                    list[index] = InvokeOperator(0);
                    index = -1;
                }
            }
        }

        internal UnaryOperator(int p,Func<long,long> f)
        {
            precedence = p;
            InvokeOperator = f;
        }

        internal static readonly Dictionary<string, Func<IOperator>> Factory = new Dictionary<string, Func<IOperator>>()
        {
            {"-", () => new UnaryOperator(1, a =>  -a) },
            {"~", () => new UnaryOperator(1, a =>  ~a) },
            {"!", () => new UnaryOperator(1, a =>  a==0 ? 1 : 0) }
        };
    }
}
