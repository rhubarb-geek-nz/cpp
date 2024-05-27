// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

namespace RhubarbGeekNz.CPreProcessor
{
    internal class CommentPipeline : IStringPipeline
    {
        readonly IStringPipeline output;
        readonly Processor cpp;
        bool inComment = false;

        internal CommentPipeline(Processor cpp, IStringPipeline output)
        {
            this.cpp = cpp;
            this.output = output;
        }

        public void Dispose()
        {
            if (inComment)
            {
                using (var o = output)
                {
                    throw new CPreProcessorException(cpp, "comment not closed");
                }
            }
            else
            {
                output.Dispose();
            }
        }

        public void Write(string s)
        {
            char[] line = s.ToCharArray();
            int i = 0, j = 0;
            int length = line.Length;
            bool inQuote = false;
            char quoteChar = (char)0;
            bool cppComment = false;

            while (i < length)
            {
                char c = line[i++];

                if (inComment)
                {
                    if ((c == '*') && (i < length) && (line[i] == '/'))
                    {
                        inComment = false;
                        i++;
                    }
                }
                else
                {
                    if (inQuote)
                    {
                        if (c == quoteChar)
                        {
                            inQuote = false;
                        }
                        else
                        {
                            if (c == '\\')
                            {
                                line[j++] = c;

                                c = line[i++];
                            }
                        }

                        line[j++] = c;
                    }
                    else
                    {
                        if ((c == '/') && (i < length))
                        {
                            c = line[i];

                            if (c == '*')
                            {
                                inComment = true;
                                i++;
                            }
                            else
                            {
                                if (c == '/')
                                {
                                    cppComment = true;
                                    break;
                                }
                                else
                                {
                                    line[j++] = '/';
                                }
                            }
                        }
                        else
                        {
                            if (c=='\"' || c == '\'')
                            {
                                inQuote = true;
                                quoteChar = c;
                            }

                            line[j++] = c;
                        }
                    }
                }
            }

            if (j == 0)
            {
                s = (inComment || cppComment) && !cpp.OutputBlankLines ? null : string.Empty;
            }
            else
            {
                s = new string(line, 0, j);
            }

            if (inQuote)
            {
                throw new CPreProcessorException(cpp, "quoted text not closed");
            }

            if (s != null)
            {
                output.Write(s);
            }
        }
    }
}
