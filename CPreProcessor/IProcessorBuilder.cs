// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace RhubarbGeekNz.CPreProcessor
{
    public interface IProcessorBuilder
    {
        string[] IncludePath { get; }
        string[] IncludeDirectory { get; }
        bool TriGraphs { get; }
        bool BlankLines { get; }
        bool LineNumbers { get; }
        IDictionary<string, string> Defines { get; }
        IStringPipeline OutputWriter { get; }
        Func<Processor, string, bool> WarningHandler { get; }
        Func<Processor, string, bool> ErrorHandler { get; }
        IProcessorBuilder UseTriGraphs(bool withTriGraphs);
        IProcessorBuilder UseBlankLines(bool withBlankLines);
        IProcessorBuilder UseLineNumbers(bool withLineNumbers);
        IProcessorBuilder UseIncludePath(string path);
        IProcessorBuilder UseIncludeDirectory(string path);
        IProcessorBuilder UseErrorHandler(Func<Processor, string, bool> warning);
        IProcessorBuilder UseWarningHandler(Func<Processor, string, bool> warning);
        IProcessorBuilder UseOutputWriter(IStringPipeline stringPipeline);
        IProcessorBuilder UseMacroDefinition(string macro, string definition);
        IStringPipeline Build();
    }
}
