// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RhubarbGeekNz.CPreProcessor
{
    public class Processor : IStringPipeline
    {
        internal readonly bool OutputBlankLines;
        internal readonly string[] IncludeDirectory;
        internal readonly string[] IncludePath;
        readonly IStringPipeline writeOutput;
        readonly LinePipeline linePipeline;
        readonly List<ConditionalState> conditionalStack = new List<ConditionalState>();
        readonly Func<Processor, string, bool> warning;
        readonly Func<Processor, string, bool> error;
        readonly Dictionary<string, IDefinedMacro> definedMacros = new Dictionary<string, IDefinedMacro>();
        readonly Tokenizer tokenizer;
        const string DEFINED = "defined";
        internal string file = "\"<stdin>\"";
        internal int line = 0;

        static internal Dictionary<string, Func<Processor, string, bool>> VerbHandlers = new Dictionary<string, Func<Processor, string, bool>>
        {
            { "include", (p,s) => p.HandleInclude(s) },
            { "define", (p,s) => p.HandleDefine(s) },
            { "undef", (p,s) => p.HandleUnDef(s) },
            { "if", (p,s) => p.HandleIf(s) },
            { "ifdef", (p,s) => p.HandleIfDef(s) },
            { "ifndef", (p,s) => p.HandleIfNDef(s) },
            { "else", (p,s) => p.HandleElse(s) },
            { "elif", (p,s) => p.HandleElIf(s) },
            { "endif", (p,s) => p.HandleEndIf(s) },
            { "error", (p,s) => p.HandleError(s) },
            { "warning", (p,s) => p.HandleWarning(s) }
        };

        public static IProcessorBuilder CreateBuilder()
        {
            return new ProcessorBuilder((t) => new Processor(t));
        }

        protected Processor(IProcessorBuilder builder)
        {
            var writeOutput = builder.OutputWriter;
            if (builder.LineNumbers)
            {
                writeOutput = linePipeline = new LinePipeline(this, writeOutput);
            }
            writeOutput = new MacroProcessor(this, writeOutput);
            writeOutput = new CommentPipeline(this, writeOutput);
            writeOutput = new LineJoiner(this, writeOutput);
            if (builder.TriGraphs)
            {
                writeOutput = new TrigraphPipeline(writeOutput);
            }
            this.writeOutput = writeOutput;
            warning = builder.WarningHandler;
            error = builder.ErrorHandler;
            tokenizer = new Tokenizer(this);
            OutputBlankLines = builder.BlankLines;

            definedMacros.Add("__FILE__", new FileMacro(this));
            definedMacros.Add("__LINE__", new LineMacro(this));

            foreach (var macro in builder.Defines)
            {
                string name = macro.Key;
                string value = macro.Value;
                definedMacros.Add(name, new DefinedMacro(name, null, value.ToArray()));
            }

            IncludeDirectory = builder.IncludeDirectory;
            IncludePath = builder.IncludePath;
        }

        public void Dispose()
        {
            line++;

            if (conditionalStack.Any())
            {
                using (var o = writeOutput)
                {
                    throw new CPreProcessorException(this, "missing #endif");
                }
            }
            else
            {
                writeOutput.Dispose();
            }
        }

        public void Write(string text)
        {
            line++;
            writeOutput.Write(text);
        }

        bool IfDefIdentifier(string s)
        {
            char[] line = s.ToCharArray();
            int length = line.Length;
            int i = 0;

            if (i < length && char.IsWhiteSpace(line[i])) i += tokenizer.ExpressionLength(line, i, length);

            if (!tokenizer.IsIdentifier(line, i, length, out int len))
            {
                throw new CPreProcessorException(this, "missing define");
            }

            bool exists = definedMacros.TryGetValue(new string(line, i, len), out var macro);

            i += len;

            if (i < length && char.IsWhiteSpace(line[i])) i += tokenizer.ExpressionLength(line, i, length);

            if (i != length)
            {
                throw new CPreProcessorException(this, "extra after define");
            }

            return exists;
        }

        bool HandleIf(string s)
        {
            bool ifCondition = IfCondition(s);
            conditionalStack.Add(new ConditionalState(!ifCondition, !ifCondition));
            return true;
        }

        bool HandleIfDef(string s)
        {
            bool ifCondition = IfDefIdentifier(s);
            conditionalStack.Add(new ConditionalState(!ifCondition, !ifCondition));
            return true;
        }

        bool HandleUnDef(string s)
        {
            definedMacros.Remove(s.Trim());
            return true;
        }

        bool HandleIfNDef(string s)
        {
            bool ifCondition = IfDefIdentifier(s);
            conditionalStack.Add(new ConditionalState(ifCondition, ifCondition));
            return true;
        }

        bool HandleInclude(string s)
        {
            if (IsConditionalBlocked())
            {
                return true;
            }

            char[] line = s.ToCharArray();
            int length = line.Length;
            int i = 0;
            StreamReader stream = null;

            try
            {
                string fileLine = null;
                string dirChar = Path.DirectorySeparatorChar.ToString();

                while (i < length && char.IsWhiteSpace(line[i])) i++;

                if (i == length) throw new CPreProcessorException(this, "include error");

                char c = line[i];

                if (c == '\"' && tokenizer.IsStringLiteral(line, i, length, out int l))
                {
                    if (IncludeDirectory == null) throw new CPreProcessorException(this, "no include directory");

                    string name = new string(line, i + 1, l - 2);
                    fileLine = new string(line, i, l);

                    i += l;

                    while (i < length && char.IsWhiteSpace(line[i])) i++;

                    if (i != length) throw new CPreProcessorException(this, "include error");

                    foreach (string dir in IncludeDirectory)
                    {
                        string filePath = String.Join(dirChar, new string[] { dir, name });

                        try
                        {
                            stream = new StreamReader(filePath);
                        }
                        catch (FileNotFoundException)
                        {
                        }

                        if (stream != null)
                        {
                            break;
                        }
                    }

                    if (stream == null)
                    {
                        throw new CPreProcessorException(this, $"include error, file {name} not found", new FileNotFoundException($"File Not Found {name}", name));
                    }
                }
                else
                {
                    if (c == '<')
                    {
                        if (IncludePath == null) throw new CPreProcessorException(this, "no include path");

                        int k = i;

                        while (k < length && line[k] != '>') k++;

                        if (k == length || line[k] != '>')
                        {
                            throw new CPreProcessorException(this, "include error");
                        }

                        string name = new string(line, i + 1, k - i - 1);

                        i += k + 1;

                        while (i < length && char.IsWhiteSpace(line[i])) i++;

                        if (i != length) throw new CPreProcessorException(this, "include error");

                        foreach (string dir in IncludePath)
                        {
                            string filePath = String.Join(dirChar, new string[] { dir, name });

                            try
                            {
                                stream = new StreamReader(filePath);
                            }
                            catch (FileNotFoundException)
                            {
                            }

                            if (stream != null)
                            {
                                fileLine = $"\"{filePath}\"";
                                break;
                            }
                        }

                        if (stream == null)
                        {
                            throw new CPreProcessorException(this, $"include error, file {name} not found", new FileNotFoundException($"File Not Found {name}", name));
                        }
                    }
                }

                if (stream == null)
                {
                    throw new CPreProcessorException(this, $"include error {s}");
                }
                else
                {
                    ProcessInclude(fileLine, stream);
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }

            return false;
        }

        private void ProcessInclude(string fileLine, StreamReader stream)
        {
            string file = this.file;
            int line = this.line;

            if (linePipeline != null)
            {
                linePipeline.WriteLineNumber();
            }

            try
            {
                this.file = fileLine;
                this.line = 0;

                if (linePipeline != null)
                {
                    linePipeline.currentLine = -1;
                }

                string s;

                while ((s = stream.ReadLine()) != null)
                {
                    Write(s);
                }
            }
            finally
            {
                if (linePipeline != null)
                {
                    linePipeline.currentLine = -1;
                }

                this.file = file;
                this.line = line;
            }
        }

        bool HandleDefine(string s)
        {
            char[] line = s.ToCharArray();
            int length = line.Length;
            int i = 0;

            while (i < length && Char.IsWhiteSpace(line[i]))
            {
                i++;
            }

            int labelStart = i;

            if (!tokenizer.IsIdentifier(line, i, length, out int labelLength))
            {
                throw new CPreProcessorException(this, $"#define {s.Trim()}");
            }

            i += labelLength;

            string label = new string(line, labelStart, labelLength);

            if (definedMacros.TryGetValue(label, out var macro))
            {
                throw new CPreProcessorException(this, $"definition for '{label}' already exists");
            }

            List<string> parameters = null;

            if (i < length && '(' == line[i])
            {
                bool isClosed = false;
                parameters = new List<string>();

                i++;

                while (true)
                {
                    while (i < length && char.IsWhiteSpace(line[i]))
                    {
                        i++;
                    }

                    if (i < length)
                    {
                        char c = line[i];

                        if (c == ')')
                        {
                            isClosed = true;
                            i++;
                            break;
                        }

                        if (c == ',')
                        {
                            i++;
                        }
                        else
                        {
                            if (!tokenizer.IsIdentifier(line, i, length, out int j))
                            {
                                throw new CannotUnloadAppDomainException($"bad argument {parameters.Count + 1} for definition {label}");
                            }

                            string argName = new string(line, i, j);

                            i += j;

                            parameters.Add(argName);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (!isClosed)
                {
                    throw new CannotUnloadAppDomainException($"mismatched argument list for definition {label}");
                }
            }

            while (i < length && Char.IsWhiteSpace(line[i]))
            {
                i++;
            }

            char[] value = new char[length - i];

            if (value.Length > 0)
            {
                Array.Copy(line, i, value, 0, value.Length);
            }

            definedMacros.Add(label, new DefinedMacro(label, parameters, value));

            return true;
        }

        bool HandleElse(string s)
        {
            if (conditionalStack.Count == 0)
            {
                throw new CPreProcessorException(this, "#else with no #if");
            }

            ConditionalState c = conditionalStack.Last();

            if (c.elsePermitted)
            {
                c.elsePermitted = false;
                c.blocked = false;
            }
            else
            {
                c.blocked = true;
            }

            return true;
        }

        bool HandleElIf(string s)
        {
            if (conditionalStack.Count == 0)
            {
                throw new CPreProcessorException(this, "#elf with no #if");
            }

            ConditionalState c = conditionalStack.Last();

            if (c.elsePermitted)
            {
                c.elsePermitted = !IfCondition(s);
                c.blocked = c.elsePermitted;
            }
            else
            {
                c.blocked = true;
            }

            return true;
        }

        bool HandleEndIf(string s)
        {
            int count = conditionalStack.Count;

            if (count > 0)
            {
                conditionalStack.RemoveRange(count - 1, 1);
            }
            else
            {
                throw new CPreProcessorException(this, "mismatched #endif");
            }

            return true;
        }

        bool HandleError(string s)
        {
            return error(this, s);
        }

        bool HandleWarning(string s)
        {
            return warning(this, s);
        }

        internal bool IsConditionalBlocked()
        {
            return conditionalStack.Where(c => c.blocked).Any();
        }

        internal string ExpandMacros(string s)
        {
            return ExpandMacros(s.ToCharArray());
        }

        internal string ExpandMacros(char[] line)
        {
            StringBuilder sb = new StringBuilder();
            int i = 0, length = line.Length;

            while (i < length)
            {
                if (tokenizer.IsIdentifier(line, i, length, out int j))
                {
                    int l = i;
                    string name = new string(line, i, j);
                    i += j;

                    if (definedMacros.TryGetValue(name, out var macro))
                    {
                        if (macro.Parameters != null)
                        {
                            List<string> args = new List<string>();

                            while (i < length)
                            {
                                if (char.IsWhiteSpace(line[i]))
                                {
                                    i++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (i == length || '(' != line[i])
                            {
                                sb.Append(name);
                            }
                            else
                            {
                                i++;

                                int argStart = i;

                                while ((i < length) && (argStart < length))
                                {
                                    int k = tokenizer.ExpressionLength(line, i, length);
                                    char c = line[i];

                                    if (c == ')' || c == ',')
                                    {
                                        if (macro.Parameters.Count > 0)
                                        {
                                            while ((argStart < i) && char.IsWhiteSpace(line[argStart]))
                                            {
                                                argStart++;
                                            }

                                            int argEnd = i;

                                            while ((argEnd > argStart) && char.IsWhiteSpace(line[argEnd - 1]))
                                            {
                                                argEnd--;
                                            }

                                            args.Add(new string(line, argStart, argEnd - argStart));
                                        }

                                        i++;
                                        argStart = c == ',' ? i : length;
                                    }
                                    else
                                    {
                                        i += k;
                                    }
                                }

                                if (macro.Parameters.Count != args.Count)
                                {
                                    throw new CPreProcessorException(this, $"macro argument count wrong");
                                }

                                sb.Append(ExpandMacro(macro, args));
                            }
                        }
                        else
                        {
                            sb.Append(ExpandMacros(macro.Value));
                        }
                    }
                    else
                    {
                        sb.Append(line, l, i - l);
                    }
                }
                else
                {
                    char c = line[i];

                    if (c == '\'' || c == '\"')
                    {
                        j = tokenizer.ExpressionLength(line, i, length);
                        sb.Append(line, i, j);
                        i += j;
                    }
                    else
                    {
                        sb.Append(c);
                        i++;
                    }
                }
            }

            return sb.ToString();
        }

        static readonly Dictionary<char, string> StringEscapes = new Dictionary<char, string>()
        {
            { '\\', "\\\\"},
            { '\n', "\\n"},
            { '\b', "\\b"},
            { '\t', "\\t"},
            { '\'', "\\\'"},
            { '\"', "\\\""}
        };

        string ExpandMacro(IDefinedMacro macro, List<string> args)
        {
            Dictionary<string, string> argValues = new Dictionary<string, string>();
            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < macro.Parameters.Count)
            {
                argValues.Add(macro.Parameters[i], args[i]);
                i++;
            }

            i = 0;

            char[] line = macro.Value;
            int length = line.Length;

            while (i < length)
            {
                if (tokenizer.IsIdentifier(line, i, length, out int j))
                {
                    string token = new string(line, i, j);
                    i += j;

                    if (argValues.TryGetValue(token, out string value))
                    {
                        token = value;

                        while (tokenizer.IsTokenPaste(line, i, length, out j, out int k))
                        {
                            i += j;

                            string appendage = new string(line, i, k);

                            i += k;

                            if (argValues.TryGetValue(appendage, out value))
                            {
                                appendage = value;
                            }

                            token += appendage;
                        }

                        token = ExpandMacros(token);
                    }

                    sb.Append(token);
                }
                else
                {
                    char c = line[i];

                    if (c == '\"' || c == '\'' || char.IsWhiteSpace(c) || char.IsDigit(c))
                    {
                        j = tokenizer.ExpressionLength(line, i, length);
                        sb.Append(line, i, j);
                        i += j;
                    }
                    else
                    {
                        if (c == '#')
                        {
                            int hashIndex = i;

                            i++;

                            while (i < length && char.IsWhiteSpace(line[i]))
                            {
                                i++;
                            }

                            if (tokenizer.IsIdentifier(line, i, length, out j) && argValues.TryGetValue(new string(line, i, j), out string value))
                            {
                                i += j;

                                sb.Append('\"');

                                foreach (char v in value)
                                {
                                    string a = null;

                                    if (v < 0 || v > 255)
                                    {
                                        sb.Append("\\u");
                                        a = ((int)v).ToString("X4");
                                    }
                                    else
                                    {
                                        if (!StringEscapes.TryGetValue(v, out a))
                                        {
                                            if (v < 32 || v > 126)
                                            {
                                                sb.Append("\\x");
                                                a = ((int)v).ToString("X2");
                                            }
                                            else
                                            {
                                                sb.Append(v);
                                                a = null;
                                            }
                                        }
                                    }

                                    if (a != null)
                                    {
                                        sb.Append(a);
                                    }
                                }

                                sb.Append('\"');
                            }
                            else
                            {
                                sb.Append(line, hashIndex, i - hashIndex);
                            }
                        }
                        else
                        {
                            sb.Append(c);
                            i++;
                        }
                    }
                }
            }

            return ExpandMacros(sb.ToString());
        }

        internal bool IfCondition(string condition)
        {
            return 0 != EvaluateCondition(condition);
        }

        protected long EvaluateCondition(string s)
        {
            string exp = ExpandCondition(s);
            char[] line = exp.ToCharArray();
            int length = line.Length;

            long value = EvaluateConditionCharSpan(line, 0, length, false, out int j);

            if (length != j)
            {
                throw new CPreProcessorException(this, "evaluation error");
            }

            return value;
        }

        string ExpandCondition(string s)
        {
            bool doExpand = true;

            while (doExpand)
            {
                int i = 0;
                char[] line = s.ToCharArray();
                int length = line.Length;
                StringBuilder sb = new StringBuilder();

                doExpand = false;

                while (i < length)
                {
                    char c = line[i];

                    if (tokenizer.IsToken(line, i, length, out int j))
                    {
                        if (char.IsDigit(c))
                        {
                            sb.Append(line, i, j);
                            i += j;
                        }
                        else
                        {
                            string id = new string(line, i, j);
                            i += j;

                            if (DEFINED.Equals(id))
                            {

                                while (i < length && char.IsWhiteSpace(line[i])) i++;

                                bool isBracket = (i < length && line[i] == '(');

                                if (isBracket)
                                {
                                    i++;

                                    while (i < length && char.IsWhiteSpace(line[i])) i++;
                                }

                                if (!tokenizer.IsIdentifier(line, i, length, out j))
                                {
                                    throw new CPreProcessorException(this, "missing identifier token for defined");
                                }

                                id = new string(line, i, j);

                                i += j;

                                if (isBracket)
                                {
                                    while (i < length && char.IsWhiteSpace(line[i])) i++;

                                    isBracket = (i < length && line[i] == ')');

                                    if (!isBracket) throw new CPreProcessorException(this, "missing closing bracket");

                                    i++;
                                }

                                sb.Append(definedMacros.ContainsKey(id) ? '1' : '0');
                            }
                            else
                            {
                                if (definedMacros.TryGetValue(id, out IDefinedMacro macro))
                                {
                                    if (macro.Parameters != null)
                                    {
                                        throw new CPreProcessorException(this, "macro cannot have parameters");
                                    }

                                    doExpand = true;

                                    sb.Append(macro.Value);
                                }
                                else
                                {
                                    sb.Append('0');
                                }
                            }
                        }
                    }
                    else
                    {
                        i++;

                        sb.Append(c);
                    }
                }

                s = sb.ToString();
            }

            return s;
        }

        long EvaluateConditionCharSpan(char[] line, int offset, int length, bool endOnBracket, out int len)
        {
            try
            {
                List<object> list = CollectTokens(line, offset, length, endOnBracket, out len);

                list.Where(o => o is IOperator).Select(o => (IOperator)o).OrderBy(o => o.Precedence).ToList().ForEach(op => op.Invoke(list));

                return (long)list.Single();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new CPreProcessorException(this, "condition evaluation error", ex);
            }
            catch (NullReferenceException ex)
            {
                throw new CPreProcessorException(this, "condition evaluation error", ex);
            }
        }

        List<object> CollectTokens(char[] line, int offset, int length, bool endOnBracket, out int len)
        {
            List<object> list = new List<object>();
            int i = offset;

            while (i < length)
            {
                char c = line[i];

                if (endOnBracket && ')' == c)
                {
                    endOnBracket = false;
                    i++;
                    break;
                }

                if (char.IsWhiteSpace(c))
                {
                    i++;
                }
                else
                {
                    if ('(' == c)
                    {
                        i++;

                        list.Add(EvaluateConditionCharSpan(line, i, length, true, out int j));

                        i += j;
                    }
                    else
                    {
                        if (tokenizer.IsToken(line, i, length, out int j))
                        {
                            if (!char.IsDigit(c))
                            {
                                string token = new string(line, i, j);
                                throw new CPreProcessorException(this, $"unknown token {token}");
                            }

                            long value;

                            if (j > 1 && c == '0')
                            {
                                if (line[i + 1] == 'x')
                                {
                                    string token = new string(line, i + 2, j - 2);
                                    value = long.Parse(token, System.Globalization.NumberStyles.HexNumber);
                                }
                                else
                                {
                                    string token = new string(line, i + 1, j - 1);
                                    value = Convert.ToInt64(token, 8);
                                }
                            }
                            else
                            {
                                string token = new string(line, i, j);
                                value = long.Parse(token);
                            }

                            list.Add(value);
                            i += j;
                        }
                        else
                        {
                            Func<IOperator> factory = null;

                            if ((list.Count == 0) || (list.Last() is IOperator))
                            {
                                string id = new string(line, i, 1);
                                i++;

                                if (!UnaryOperator.Factory.TryGetValue(id, out factory))
                                {
                                    throw new CPreProcessorException(this, $"unknown token {id}");
                                }
                            }
                            else
                            {
                                int nameLen = 0;

                                foreach (var pair in BinaryOperator.Factory)
                                {
                                    string name = pair.Key;

                                    if (name.Length > nameLen)
                                    {
                                        int k = 0;
                                        int l = name.Length;

                                        while (k < l && i + k < length)
                                        {
                                            if (line[i + k] != name[k])
                                            {
                                                break;
                                            }

                                            k++;
                                        }

                                        if ((k == l) && (k > nameLen))
                                        {
                                            factory = pair.Value;
                                            nameLen = k;

                                            if (k == BinaryOperator.MaxNameLen)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (nameLen == 0)
                                {
                                    throw new CPreProcessorException(this, "unknown operator");
                                }

                                i += nameLen;
                            }

                            list.Add(factory());
                        }
                    }
                }
            }

            if (endOnBracket)
            {
                throw new CPreProcessorException(this, "bracked mismatch");
            }

            len = i - offset;

            return list;
        }
    }
}
