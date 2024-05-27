// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    public class ProcessorBuilder : IProcessorBuilder
    {
        private readonly List<string> includesPaths = new List<string>(), includesDirectories = new List<string>();
        private readonly Dictionary<string, string> defines = new Dictionary<string, string>();
        private bool triGraphs, blankLines, lineNumbers;
        private Func<Processor, string, bool> warningHandler, errorHandler;
        private IStringPipeline outputWriter;
        private Func<IProcessorBuilder, Processor> CreateInstance;

        public string[] IncludePath => includesPaths.ToArray();

        public string[] IncludeDirectory => includesDirectories.ToArray();

        public bool TriGraphs => triGraphs;

        public bool BlankLines => blankLines;

        public bool LineNumbers => lineNumbers;

        public IDictionary<string, string> Defines => defines;

        public IStringPipeline OutputWriter => outputWriter;

        public Func<Processor, string, bool> WarningHandler => warningHandler;

        public Func<Processor, string, bool> ErrorHandler => errorHandler;

        public ProcessorBuilder(Func<IProcessorBuilder, Processor> func)
        {
            CreateInstance = func;
        }

        public IStringPipeline Build()
        {
            return CreateInstance(this);
        }

        public IProcessorBuilder UseBlankLines(bool withBlankLines)
        {
            blankLines = withBlankLines;
            return this;
        }
        public IProcessorBuilder UseLineNumbers(bool withLineNubers)
        {
            lineNumbers = withLineNubers;
            return this;
        }

        public IProcessorBuilder UseErrorHandler(Func<Processor, string, bool> error)
        {
            errorHandler = error;
            return this;
        }

        public IProcessorBuilder UseIncludeDirectory(string path)
        {
            includesDirectories.Add(path);
            return this;
        }

        public IProcessorBuilder UseIncludePath(string path)
        {
            includesPaths.Add(path);
            return this;
        }

        public IProcessorBuilder UseOutputWriter(IStringPipeline stringPipeline)
        {
            outputWriter = stringPipeline;
            return this;
        }

        public IProcessorBuilder UseTriGraphs(bool withTriGraphs)
        {
            triGraphs = withTriGraphs;
            return this;
        }

        public IProcessorBuilder UseWarningHandler(Func<Processor, string, bool> warning)
        {
            warningHandler = warning;
            return this;
        }

        public IProcessorBuilder UseMacroDefinition(string macro, string definition)
        {
            defines.Add(macro, definition);
            return this;
        }
    }
}
