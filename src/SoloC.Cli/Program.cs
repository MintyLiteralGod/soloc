using SoloC.Compiler;
using SoloC.Compiler.Diagnostics;
using SoloC.Compiler.Runtime;

return Run(args);

static int Run(string[] args)
{
    if (args.Length == 0 || args[0] is "-h" or "--help")
    {
        PrintHelp();
        return args.Length == 0 ? 1 : 0;
    }

    var command = args[0];
    return command switch
    {
        "run" => RunFile(RequirePath(args, "run")),
        "parse" => ParseFile(RequirePath(args, "parse")),
        "repl" => RunRepl(),
        "version" or "--version" or "-v" => PrintVersion(),
        _ when File.Exists(command) => RunFile(command),
        _ => Unknown(command),
    };
}

static string RequirePath(string[] args, string command)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine($"Usage: soloc {command} <file.sc>");
        Environment.Exit(1);
    }

    return args[1];
}

static int RunFile(string path)
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"File not found: {path}");
        return 1;
    }

    var compilation = Compilation.FromFile(path);
    var result = compilation.Evaluate(Console.Out);
    PrintDiagnostics(result.Diagnostics, path);

    return result.Success ? 0 : 1;
}

static int ParseFile(string path)
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"File not found: {path}");
        return 1;
    }

    var compilation = Compilation.FromFile(path);
    var result = compilation.Parse();
    PrintDiagnostics(result.Diagnostics, path);

    if (!result.Success)
        return 1;

    Console.WriteLine($"OK: parsed {result.Tree.Members.Count} top-level member(s) from {path}");
    return 0;
}

static int RunRepl()
{
    Console.WriteLine("SoloC REPL — type :quit to exit");
    while (true)
    {
        Console.Write(">>> ");
        var line = Console.ReadLine();
        if (line is null || line is ":quit" or ":exit")
            break;

        if (string.IsNullOrWhiteSpace(line))
            continue;

        var compilation = new Compilation(line, "<repl>");
        var result = compilation.Evaluate(Console.Out);
        PrintDiagnostics(result.Diagnostics, "<repl>");

        if (result.Success && result.Value.Kind is not SoloValueKind.Void
            and not SoloValueKind.Null)
        {
            Console.WriteLine($"=> {result.Value}");
        }
    }

    return 0;
}

static int PrintVersion()
{
    Console.WriteLine("SoloC 0.1.0 — SoloGem language (C#-inspired)");
    return 0;
}

static int Unknown(string command)
{
    Console.Error.WriteLine($"Unknown command: {command}");
    PrintHelp();
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine(
        """
        SoloC — SoloGem's developer language (based on C#)

        Usage:
          soloc run <file.sc>     Execute a SoloC program
          soloc parse <file.sc>   Parse and validate syntax
          soloc repl              Interactive prompt
          soloc version           Print version
          soloc <file.sc>         Shortcut for run

        Examples:
          soloc run examples/hello.sc
          soloc repl
        """);
}

static void PrintDiagnostics(IReadOnlyList<Diagnostic> diagnostics, string path)
{
    foreach (var diagnostic in diagnostics)
    {
        var writer = diagnostic.Severity == DiagnosticSeverity.Error ? Console.Error : Console.Out;
        writer.WriteLine($"{path}({diagnostic.Span.Start}): {diagnostic.Severity.ToString().ToLowerInvariant()}: {diagnostic.Message}");
    }
}
