// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace RhubarbGeekNz.CPreProcessor
{
    internal class BinaryOperator : IOperator
    {
        private readonly int precedence;
        public int Precedence => precedence;

        private readonly Func<long, long, long> InvokeOperator;

        public void Invoke(List<object> list)
        {
            int index = list.IndexOf(this)-1;
            list[index] = InvokeOperator((long)list[index], (long)list[index + 2]);
            list.RemoveRange(index+1 ,2);
        }

        internal BinaryOperator(int p, Func<long, long, long> f)
        {
            precedence = p;
            InvokeOperator = f;
        }

        internal static readonly Dictionary<string, Func<IOperator>> Factory = new Dictionary<string, Func<IOperator>>()
        {
            {"*", () => new BinaryOperator(2, (a,b) => a * b) },
            {"/", () => new BinaryOperator(2, (a,b) => a / b) },
            {"%", () => new BinaryOperator(2, (a,b) => a % b) },
            {"+", () => new BinaryOperator(3, (a,b) =>  a + b) },
            {"-", () => new BinaryOperator(3, (a,b) =>  a - b) },
            {"<<", () => new BinaryOperator(4, (a,b) =>  a << (int)b) },
            {">>", () => new BinaryOperator(4, (a,b) =>  a >> (int)b) },
            {">=", () => new BinaryOperator(5, (a,b) => (a >= b)?1:0) },
            {"<=", () => new BinaryOperator(5, (a,b) => (a <= b)?1:0) },
            {">", () => new BinaryOperator(5, (a,b) => (a > b)?1:0) },
            {"<", () => new BinaryOperator(5, (a,b) => (a < b)?1:0) },
            {"!=", () => new BinaryOperator(6, (a,b) => (a != b)?1:0) },
            {"==", () => new BinaryOperator(6, (a,b) => (a == b)?1:0) },
            {"&", () => new BinaryOperator(7, (a,b) =>  a & b) },
            {"^", () => new BinaryOperator(8, (a,b) =>  a ^ b) },
            {"|", () => new BinaryOperator(9, (a,b) =>  a | b) },
            {"&&", () => new BinaryOperator(10, (a,b) => (a!=0 && b!=0)?1:0) },
            {"||", () => new BinaryOperator(10, (a,b) => (a!=0 || b!=0)?1:0) },
            {"?", () => new IfOperator() },
            {":", () => new ElseOperator() }
        };

        internal static readonly int MaxNameLen = Factory.Keys.Max(o => o.Length);
    }
}
