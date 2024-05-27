// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

using RhubarbGeekNz.CPreProcessor;

var builder = Processor
    .CreateBuilder()
    .UseOutputWriter(new StringPipeline(
        s => Console.Out.WriteLine(s)
    ))
    .UseWarningHandler((c, s) =>
    {
        Console.Error.WriteLine(s);
        return false;
    })
    .UseErrorHandler((c, s) =>
    {
        throw new CPreProcessorException(c, s);
    })
    .UseIncludeDirectory(Environment.CurrentDirectory);

bool lineNumbers = true;

foreach (var arg in args)
{
    if (arg[0] != '-')
    {
        throw new ArgumentException($"Unrecognized argument {arg}");
    }

    switch (arg[1])
    {
        case 'D':
            {
                int i = arg.IndexOf('=');

                if (i < 0)
                {
                    builder = builder.UseMacroDefinition(arg.Substring(2), "1");
                }
                else
                {
                    builder = builder.UseMacroDefinition(arg.Substring(2, i - 2), arg.Substring(i + 1));
                }
            }
            break;

        case 'I':
            builder = builder.UseIncludePath(arg.Substring(2));
            break;

        default:
            switch (arg)
            {
                case "-trigraphs":
                    builder = builder.UseTriGraphs(true);
                    break;

                case "-P":
                    lineNumbers = false;
                    break;

                default:
                    throw new ArgumentException($"Unrecognized argument {arg}");
            }
            break;
    }
}

using (var cpp = builder.UseLineNumbers(lineNumbers).Build())
{
    string input;

    while ((input = Console.ReadLine()) != null)
    {
        cpp.Write(input);
    }
}
