// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

namespace RhubarbGeekNz.CPreProcessor
{
    internal class TrigraphPipeline : IStringPipeline
    {
        readonly IStringPipeline output;

        internal TrigraphPipeline(IStringPipeline output)
        {
            this.output = output;
        }

        public void Write(string s)
        {
            if (s.Contains("??"))
            {
                char[] line = s.ToCharArray();
                int i = 0, j = 0;
                int length = line.Length - 2;

                while (i < length)
                {
                    char c = line[i++];

                    if ((c == '?') && (line[i] == '?'))
                    {
                        switch (line[i+1])
                        {
                            case '=': i += 2; c = '#'; break;
                            case '/': i += 2; c = '\\'; break;
                            case '\'': i += 2; c = '^'; break;
                            case '(': i += 2; c = '['; break;
                            case ')': i += 2; c = ']'; break;
                            case '<': i += 2; c = '{'; break;
                            case '>': i += 2; c = '}'; break;
                            case '-': i += 2; c = '~'; break;
                            case '!': i += 2; c = '|'; break;
                            default:
                                break;
                        }
                    }

                    line[j++] = c;
                }

                length = line.Length;

                while (i < length)
                {
                    line[j++] = line[i++];
                }

                s = new string(line, 0, j);
            }

            output.Write(s);
        }

        public void Dispose()
        {
            output.Dispose();
        }
    }
}
