// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

namespace RhubarbGeekNz.CPreProcessor
{
    internal class LineJoiner : IStringPipeline
    {
        readonly IStringPipeline output;
        readonly Processor cpp;
        string previousLine;

        public LineJoiner(Processor cpp, IStringPipeline output)
        {
            this.cpp = cpp;
            this.output = output;
        }

        public void Dispose()
        {
            try
            {
                if (previousLine != null)
                {
                    throw new CPreProcessorException(cpp, "line continuation at end of data");
                }
            }
            finally
            {
                output.Dispose();
            }
        }

        public void Write(string s)
        {
            if (previousLine != null)
            {
                s = previousLine + s;

                previousLine = null;
            }

            if (s.EndsWith("\\"))
            {
                previousLine = s.Substring(0,s.Length-1);
            }
            else
            {
                output.Write(s);
            }
        }
    }
}
