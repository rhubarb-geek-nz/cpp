// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

namespace RhubarbGeekNz.CPreProcessor
{
    internal class LinePipeline : IStringPipeline
    {
        readonly IStringPipeline output;
        readonly Processor cpp;
        internal int currentLine = -1;

        internal LinePipeline(Processor cpp, IStringPipeline output)
        {
            this.cpp = cpp;
            this.output = output;
        }

        public void Dispose()
        {
            WriteLineNumber();

            output.Dispose();
        }

        public void Write(string s)
        {
            WriteLineNumber();

            output.Write(s);
        }

        internal void WriteLineNumber()
        {
            currentLine++;

            if (currentLine != cpp.line)
            {
                currentLine = cpp.line;

                output.Write($"# {currentLine} {cpp.file}");
            }
        }
    }
}
