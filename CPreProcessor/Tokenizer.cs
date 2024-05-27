// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    internal class Tokenizer
    {
        readonly Processor processor;
        internal Tokenizer(Processor processor)
        {
            this.processor = processor;
        }

        internal int ExpressionLength(char[] line, int offset, int length)
        {
            int i = offset;

            while (i < length && Char.IsWhiteSpace(line[i]))
            {
                i++;
            }

            if ((i == offset) && (i < length))
            {
                if (IsToken(line, offset, length, out int len))
                {
                    i += len;
                }
                else
                {
                    char c = line[i++];

                    if (c == '(' || c == '[')
                    {
                        char closeBracket = c == '(' ? ')' : ']';

                        while (i < length)
                        {
                            c = line[i];

                            if (closeBracket == c)
                            {
                                i++;
                                break;
                            }
                            else if ((c == ',') || char.IsWhiteSpace(c))
                            {
                                i++;
                            }
                            else
                            {
                                i += ExpressionLength(line, i, length);
                            }
                        }
                    }
                    else
                    {
                        if ((c == '#') && (i < length) && (line[i] == '#'))
                        {
                            i++;
                        }
                    }
                }
            }

            return i - offset;
        }

        internal bool IsTokenPaste(char[] line, int offset, int length, out int len, out int tokenLen)
        {
            int i = offset;
            bool result = false;

            while (i < length && char.IsWhiteSpace(line[i]))
            {
                i++;
            }

            if (i < length - 2 && line[i] == '#' && line[i + 1] == '#')
            {
                i += 2;

                while (i < length && char.IsWhiteSpace(line[i]))
                {
                    i++;
                }

                result = IsIdentifier(line, i, length, out tokenLen);

                if (result)
                {
                    len = i - offset;
                }
                else
                {
                    len = 0;
                }
            }
            else
            {
                len = 0;
                tokenLen = 0;
            }

            return result;
        }

        internal bool IsIdentifier(char[] line, int offset, int length, out int len)
        {
            int i = offset;

            if (i < length && (line[i] == '_' || char.IsLetter(line[i])))
            {
                i++;

                while (i < length)
                {
                    char c = line[i];

                    if (c == '_' || char.IsLetterOrDigit(c))
                    {
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }

                len = i - offset;
            }
            else
            {
                len = 0;
            }

            return len != 0;
        }

        static HashSet<char> exponentChars = new HashSet<char>() { 'p', 'P', 'e', 'E' };

        internal bool IsStringLiteral(char[] line, int offset, int length, out int len)
        {
            int i = offset;
            char c = line[i];

            if (c == '\'' || c == '\"')
            {
                i++;

                char quoteChar = c;
                bool midQuote = true;

                while (i < length)
                {
                    c = line[i++];

                    if (c == '\\')
                    {
                        i++;
                    }
                    else
                    {
                        if (c == quoteChar)
                        {
                            midQuote = false;

                            break;
                        }
                    }
                }

                if (midQuote)
                {
                    throw new CPreProcessorException(processor, "unterminated quote");
                }
            }

            len = i - offset;

            return i != offset;
        }

        internal bool IsNumber(char[] line, int offset, int length, out int len)
        {
            int i = offset;

            if (i < length && (line[i] == '.' || char.IsDigit(line[i])))
            {
                i++;

                while (i < length)
                {
                    char c = line[i];

                    if (c == '.' || char.IsLetterOrDigit(c))
                    {
                        i++;
                    }
                    else
                    {
                        if (((c == '+') || (c == '-')) && exponentChars.Contains(line[i - 1]))
                        {
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                len = i - offset;
            }
            else
            {
                len = 0;
            }

            return len != 0;
        }

        internal bool IsToken(char[] line, int offset, int length, out int len)
        {
            int i = offset;
            char c = line[i];

            switch (c)
            {
                case '\"':
                case '\'':
                    return IsStringLiteral(line, i, length, out len);
            }

            if (c == '_' || char.IsLetter(c))
            {
                return IsIdentifier(line, i, length, out len);
            }

            if (c == '.' || char.IsDigit(c))
            {
                return IsNumber(line, i, length, out len);
            }

            len = 0;

            return false;
        }
    }
}
