// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;

namespace RhubarbGeekNz.CPreProcessor
{
    internal class MacroProcessor : IStringPipeline
    {
        readonly Processor cpp;
        readonly IStringPipeline writer;
        internal MacroProcessor(Processor cpp, IStringPipeline writer)
        {
            this.cpp = cpp;
            this.writer = writer;
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        public void Write(string s)
        {
            int length = s.Length;
            int i = 0;
            bool isMacro = false;

            while (i < length)
            {
                char c = s[i++];

                if (!Char.IsWhiteSpace(c))
                {
                    isMacro = c == '#';

                    break;
                }
            }

            if (isMacro)
            {
                isMacro = false;

                while (i < length)
                {
                    char c = s[i];

                    if (!Char.IsWhiteSpace(c))
                    {
                        break;
                    }

                    i++;
                }

                if (i < length)
                {
                    int j = i;

                    while (j < length)
                    {
                        char c = s[j];

                        if (Char.IsWhiteSpace(c))
                        {
                            break;
                        }

                        j++;
                    }

                    string verb = s.Substring(i, j - i);

                    while (j < length)
                    {
                        char c = s[j];

                        if (!Char.IsWhiteSpace(c))
                        {
                            break;
                        }

                        j++;
                    }

                    string arg = s.Substring(j);

                    if (Processor.VerbHandlers.TryGetValue(verb, out var handler))
                    {
                        isMacro = true;

                        if (handler(cpp, arg) && cpp.OutputBlankLines)
                        {
                            writer.Write(String.Empty);
                        }
                    }
                }
            }

            if (!isMacro)
            {
                if (cpp.IsConditionalBlocked())
                {
                    if (cpp.OutputBlankLines)
                    {
                        writer.Write(string.Empty);
                    }
                }
                else
                {
                    writer.Write(cpp.ExpandMacros(s));
                }
            }
        }
    }
}
