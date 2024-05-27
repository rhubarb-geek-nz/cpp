// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

namespace RhubarbGeekNz.CPreProcessor
{
    public class SplitterPipeline : IStringPipeline
    {
        readonly IStringPipeline output;
        readonly string eol;

        public SplitterPipeline(string eol, IStringPipeline output)
        {
            this.output = output;
            this.eol = eol;
        }

        public void Dispose()
        {
            output.Dispose();
        }

        public void Write(string s)
        {
            int offset = 0;
            int length = s.Length;

            while (offset < length)
            {
                int index = s.IndexOf(eol, offset);

                if (index < 0)
                {
                    break;
                }

                output.Write(s.Substring(offset, index - offset));

                offset = index + eol.Length;
            }

            if (offset == 0)
            {
                output.Write(s);
            }
            else
            {
                output.Write(s.Substring(offset));
            }
        }
    }
}
