// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Management.Automation;

namespace RhubarbGeekNz.CPreProcessor.PowerShell
{
    [Cmdlet(VerbsLifecycle.Invoke, "CPreProcessor")]
    [OutputType(typeof(string))]
    sealed public class InvokeCPreProcessor : PSCmdlet, IDisposable
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string InputString;

        [Parameter]
        public string[] IncludePath;

        [Parameter]
        public string[] IncludeDirectory;

        [Parameter]
        public IDictionary MacroDefinitions;

        [Parameter]
        public SwitchParameter Trigraphs
        {
            get
            {
                return trigraphs;
            }

            set
            {
                trigraphs = value;
            }
        }

        [Parameter]
        public SwitchParameter BlankLines
        {
            get
            {
                return blankLines;
            }

            set
            {
                blankLines = value;
            }
        }

        [Parameter]
        public SwitchParameter LineNumbers
        {
            get
            {
                return lineNumbers;
            }

            set
            {
                lineNumbers = value;
            }
        }

        private bool lineNumbers, blankLines, trigraphs;
        private IStringPipeline writer;
        private const string UnixEOL = "\n", WindowsEOL = "\r\n";

        protected override void BeginProcessing()
        {
            var builder = Processor.
                CreateBuilder().UseOutputWriter(new StringPipeline(s => WriteObject(s))).
                UseErrorHandler((cpp, s) =>
                {
                    CPreProcessorException ex = new CPreProcessorException(cpp, s);
                    WriteError(new ErrorRecord(ex, $"#error {ex.Line} {ex.File}", ErrorCategory.FromStdErr, null));
                    return false;
                }).
                UseWarningHandler((cpp, s) =>
                {
                    WriteWarning(s);
                    return false;
                }).
                UseTriGraphs(trigraphs).
                UseLineNumbers(lineNumbers).
                UseBlankLines(blankLines);

            if (IncludeDirectory != null)
            {
                foreach (var path in IncludeDirectory)
                {
                    foreach (var directory in GetResolvedProviderPathFromPSPath(path, out var provider))
                    {
                        builder = builder.UseIncludeDirectory(directory);
                    }
                }
            }

            if (IncludePath != null)
            {
                foreach (var path in IncludePath)
                {
                    foreach (var directory in GetResolvedProviderPathFromPSPath(path, out var provider))
                    {
                        builder = builder.UseIncludePath(directory);
                    }
                }
            }

            if (MacroDefinitions != null)
            {
                foreach (var key in MacroDefinitions.Keys)
                {
                    var value = MacroDefinitions[key];
                    builder = builder.UseMacroDefinition(key.ToString(), value == null ? "1" : value.ToString());
                }
            }

            var processor = builder.Build();

            IStringPipeline unixSplitter = new SplitterPipeline(UnixEOL, new StringPipeline(s =>
            {
                try
                {
                    processor.Write(s);
                }
                catch (CPreProcessorException ex)
                {
                    WriteError(
                        new ErrorRecord(
                            new CPreProcessorException(ex),
                            $"cpp {ex.Line} {ex.File}",
                            ErrorCategory.FromStdErr,
                            null));
                }
            },
            processor));

            writer = new SplitterPipeline(WindowsEOL, unixSplitter);
        }

        protected override void ProcessRecord()
        {
            if (InputString != null)
            {
                writer.Write(InputString);
            }
        }

        protected override void EndProcessing() => Dispose();

        public void Dispose()
        {
            IDisposable disposable = writer;
            writer = null;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
