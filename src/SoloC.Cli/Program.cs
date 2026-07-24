using SoloC.Compiler;
using SoloC.Compiler.Diagnostics;
using SoloC.Compiler.Runtime;
using SoloC.Compiler.Text;
using SoloHtml.Compiler;

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
        "run" => RunFile(ParseRunArgs(args)),
        "parse" => ParseFile(RequirePath(args, "parse")),
        "check" => CheckFile(RequirePath(args, "check")),
        "html" => CompileHtml(args),
        "repl" => RunRepl(),
        "explain" => Explain(args.ElementAtOrDefault(1)),
        "version" or "--version" or "-v" => PrintVersion(),
        _ when command.EndsWith(".solohtml", StringComparison.OrdinalIgnoreCase) && File.Exists(command)
            => CompileHtml(["html", command]),
        _ when File.Exists(command) => RunFile((command, ExecutionEngine.Auto)),
        _ => Unknown(command),
    };
}

static (string Path, ExecutionEngine Engine) ParseRunArgs(string[] args)
{
    var engine = ExecutionEngine.Auto;
    string? path = null;

    for (var i = 1; i < args.Length; i++)
    {
        if (args[i] is "--engine" && i + 1 < args.Length)
        {
            engine = args[++i].ToLowerInvariant() switch
            {
                "vm" or "bytecode" => ExecutionEngine.Vm,
                "interpreter" or "tree" => ExecutionEngine.Interpreter,
                "auto" => ExecutionEngine.Auto,
                _ => engine,
            };
        }
        else if (!args[i].StartsWith('-'))
        {
            path = args[i];
        }
    }

    if (path is null)
    {
        Console.Error.WriteLine("Usage: soloc run [--engine auto|vm|interpreter] <file.sc>");
        Environment.Exit(1);
    }

    return (path, engine);
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

static int RunFile((string Path, ExecutionEngine Engine) options)
{
    if (!File.Exists(options.Path))
    {
        Console.Error.WriteLine($"File not found: {options.Path}");
        return 1;
    }

    var compilation = Compilation.FromFile(options.Path);
    var result = compilation.Evaluate(Console.Out, options.Engine);
    PrintDiagnostics(result.Diagnostics, compilation.SourceText);

    if (result.Success && options.Engine != ExecutionEngine.Auto && result.Engine is { } usedEngine)
        Console.Error.WriteLine($"[soloc] engine={usedEngine.ToString().ToLowerInvariant()}");

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
    PrintDiagnostics(result.Diagnostics, result.SourceText);

    if (!result.Success)
        return 1;

    Console.WriteLine($"OK: parsed {result.Tree.Members.Count} top-level member(s) from {path}");
    return 0;
}

static int CheckFile(string path)
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"File not found: {path}");
        return 1;
    }

    var compilation = Compilation.FromFile(path);
    // Type-check only: evaluate discarded by parsing + checker through Evaluate with a no-op when errors
    var parse = compilation.Parse();
    var diagnostics = new DiagnosticBag(compilation.SourceText);
    diagnostics.AddRange(parse.Diagnostics);
    if (parse.Success)
    {
        var checker = new SoloC.Compiler.Binding.TypeChecker(diagnostics);
        checker.Check(parse.Tree);
    }

    PrintDiagnostics(diagnostics.Diagnostics, compilation.SourceText);
    if (diagnostics.HasErrors)
        return 1;

    Console.WriteLine($"OK: {path} type-checks cleanly.");
    return 0;
}

static int CompileHtml(string[] args)
{
    // soloc html <file.solohtml> [--out file.html] [--stdout]
    string? path = null;
    string? outPath = null;
    var stdout = false;

    for (var i = 1; i < args.Length; i++)
    {
        if (args[i] is "--out" or "-o" && i + 1 < args.Length)
            outPath = args[++i];
        else if (args[i] is "--stdout")
            stdout = true;
        else if (!args[i].StartsWith('-'))
            path = args[i];
    }

    if (path is null)
    {
        Console.Error.WriteLine("Usage: soloc html <file.solohtml> [--out file.html]");
        return 1;
    }

    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"File not found: {path}");
        return 1;
    }

    var source = File.ReadAllText(path);
    var result = SoloHtmlCompiler.Compile(source);
    if (!result.Ok)
    {
        foreach (var error in result.Errors)
            Console.Error.WriteLine(error);
        return 1;
    }

    outPath ??= Path.ChangeExtension(path, ".html");
    if (stdout)
    {
        Console.Write(result.Html);
    }
    else
    {
        File.WriteAllText(outPath, result.Html);
        Console.WriteLine($"Wrote {outPath}");
    }

    return 0;
}

static int RunRepl()
{
    Console.WriteLine("SoloC REPL — made by SoloGem");
    Console.WriteLine("Type :help for tips, :quit to exit");
    while (true)
    {
        Console.Write(">>> ");
        var line = Console.ReadLine();
        if (line is null || line is ":quit" or ":exit")
            break;

        if (string.IsNullOrWhiteSpace(line))
            continue;

        if (line is ":help")
        {
            Explain("repl");
            continue;
        }

        var compilation = new Compilation(line, "<repl>");
        var result = compilation.Evaluate(Console.Out);
        PrintDiagnostics(result.Diagnostics, compilation.SourceText);

        if (result.Success && result.Value.Kind is not SoloValueKind.Void
            and not SoloValueKind.Null)
        {
            Console.WriteLine($"=> {result.Value}");
        }
    }

    return 0;
}

static int Explain(string? topic)
{
    topic = (topic ?? "welcome").ToLowerInvariant();
    var text = topic switch
    {
        "welcome" or "soloc" => """
            SoloC is SoloGem's open-source language — designed to be the easiest to learn.
            Start here: docs/learn/00-welcome.md
            """,
        "print" => """
            print("Hello");
            Console.WriteLine("Also works");
            """,
        "var" or "variables" => """
            var x = 10;     // can change later
            let y = 20;     // stays the same
            int z = 30;     // typed
            """,
        "array" or "arrays" => """
            var nums = [1, 2, 3];
            print(nums[0]);
            print(nums.Length);
            """,
        "using" or "modules" => """
            using Math;
            print(sqrt(9));
            print(max(3, 7));
            """,
        "repl" => """
            Type SoloC code and press Enter.
            Commands: :help  :quit
            """,
        "vm" => """
            SoloC can run simple scripts on a bytecode VM.
            soloc run --engine vm file.sc
            Learn more: docs/vm.md
            """,
        "html" or "solohtml" => """
            SoloHTML is SoloGem's easiest markup language.
            Write indentation-based pages, compile to HTML5:

              soloc html examples/html/hello.solohtml

            Learn more: docs/solohtml/README.md
            """,
        _ => $"No mini-lesson for '{topic}'. Try: welcome, print, variables, arrays, modules, vm, html",
    };

    Console.WriteLine(text.Trim());
    return 0;
}

static int PrintVersion()
{
    Console.WriteLine("SoloC 0.3.0 — made by SoloGem (open source, MIT) · includes SoloHTML");
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
        SoloC — the easiest language to learn, made by SoloGem

        Usage:
          soloc run [--engine auto|vm|interpreter] <file.sc>
          soloc parse <file.sc>
          soloc check <file.sc>
          soloc html <file.solohtml> [--out file.html]
          soloc explain [topic]
          soloc repl
          soloc version

        Learn:
          docs/learn/00-welcome.md
          docs/solohtml/README.md
          docs/cheatsheet.md

        Examples:
          soloc run examples/hello.sc
          soloc html examples/html/showcase.solohtml
          soloc explain html
          soloc repl
        """);
}

static void PrintDiagnostics(IReadOnlyList<Diagnostic> diagnostics, SourceText source)
{
    foreach (var diagnostic in diagnostics)
    {
        var writer = diagnostic.Severity == DiagnosticSeverity.Error ? Console.Error : Console.Out;
        var location = diagnostic.Location ?? source.GetLocation(diagnostic.Span);
        writer.WriteLine($"{source.FileName}:{location.Line}:{location.Column}: {diagnostic.Severity.ToString().ToLowerInvariant()}: {diagnostic.Message}");
        if (!string.IsNullOrWhiteSpace(diagnostic.Tip))
            writer.WriteLine($"  tip: {diagnostic.Tip}");

        if (diagnostic.Severity == DiagnosticSeverity.Error && location.Line >= 1 && location.Line <= source.LineCount)
        {
            var lineText = source.GetLine(location.Line);
            writer.WriteLine($"  {lineText}");
            var caret = Math.Max(1, location.Column);
            writer.WriteLine($"  {new string(' ', caret - 1)}^");
        }
    }
}
