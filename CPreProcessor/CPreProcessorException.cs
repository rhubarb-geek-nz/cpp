// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;

namespace RhubarbGeekNz.CPreProcessor
{
    public class CPreProcessorException : Exception
    {
        public int Line { get; }
        public string File { get; }

        public CPreProcessorException(Processor cpp,string s) : base(s)
        {
            Line = cpp.line;
            File = cpp.file;
        }

        public CPreProcessorException(CPreProcessorException ex) : base(ex.Message)
        {
            Line = ex.Line;
            File = ex.File;
        }

        public CPreProcessorException(Processor cpp, string s, Exception ex) : base(s,ex)
        {
            Line = cpp.line;
            File = cpp.file;
        }
    }
}
