// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;

namespace RhubarbGeekNz.CPreProcessor
{
    public class StringPipeline : IStringPipeline
    {
        private readonly IDisposable disposable;
        private readonly Action<string> stringWriter;

        public StringPipeline(Action<string> stringWriter, IDisposable disposable = null)
        {
            this.stringWriter = stringWriter;
            this.disposable = disposable;
        }

        public void Dispose()
        {
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        public void Write(string s)
        {
            stringWriter(s);
        }
    }
}
