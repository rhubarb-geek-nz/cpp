// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;

namespace RhubarbGeekNz.CPreProcessor
{
    public interface IStringPipeline : IDisposable
    {
        void Write(string s);
    }
}
