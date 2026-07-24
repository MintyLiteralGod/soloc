using SoloHtml.Compiler;

return Run(args);

static int Run(string[] args)
{
    if (args.Length == 0 || args[0] is "-h" or "--help")
    {
        PrintHelp();
        return args.Length == 0 ? 1 : 0;
    }

    return args[0] switch
    {
        "compile" or "c" => Compile(args),
        "watch" or "w" => Watch(args),
        "version" or "--version" or "-v" => PrintVersion(),
        "studio" => PrintStudioHint(),
        _ when args[0].EndsWith(".solohtml", StringComparison.OrdinalIgnoreCase)
            => Compile(["compile", .. args]),
        _ => Unknown(args[0]),
    };
}

static int Compile(string[] args)
{
    var (path, outPath, stdout) = ParseCompileArgs(args, requirePath: true);
    if (path is null)
        return 1;

    return CompileOnce(path, outPath, stdout, quiet: false);
}

static int Watch(string[] args)
{
    var (path, outPath, stdout) = ParseCompileArgs(args, requirePath: true);
    if (path is null)
        return 1;

    outPath ??= Path.ChangeExtension(path, ".html");
    Console.WriteLine($"Watching {path} → {outPath}");
    Console.WriteLine("Press Ctrl+C to stop.");

    CompileOnce(path, outPath, stdout: false, quiet: false);

    using var watcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(path))!, Path.GetFileName(path))
    {
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
        EnableRaisingEvents = true,
    };

    void OnChange(object _, FileSystemEventArgs __)
    {
        // Editors often bounce saves — small delay avoids partial reads.
        Thread.Sleep(80);
        CompileOnce(path, outPath, stdout: false, quiet: false);
    }

    watcher.Changed += OnChange;
    watcher.Created += OnChange;
    watcher.Renamed += OnChange;

    var exit = new ManualResetEvent(false);
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        exit.Set();
    };
    exit.WaitOne();
    Console.WriteLine("Stopped.");
    return 0;
}

static (string? Path, string? OutPath, bool Stdout) ParseCompileArgs(string[] args, bool requirePath)
{
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

    if (requirePath && path is null)
    {
        Console.Error.WriteLine("Usage: solohtml compile <file.solohtml> [--out file.html]");
        return (null, null, false);
    }

    if (path is not null && !File.Exists(path))
    {
        Console.Error.WriteLine($"File not found: {path}");
        return (null, null, false);
    }

    return (path, outPath, stdout);
}

static int CompileOnce(string path, string? outPath, bool stdout, bool quiet)
{
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
        if (!quiet)
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Wrote {outPath}");
    }

    return 0;
}

static int PrintVersion()
{
    Console.WriteLine("SoloHTML 0.1.0 — compiler by SoloGem (MIT)");
    return 0;
}

static int PrintStudioHint()
{
    Console.WriteLine(
        """
        SoloHTML Studio (GUI):

          dotnet run --project src/SoloHtml.Studio

        Then open http://localhost:5089
        """);
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
        SoloHTML — SoloGem's easiest markup language (compiles to HTML5)

        Usage:
          solohtml compile <file.solohtml> [--out file.html] [--stdout]
          solohtml watch <file.solohtml> [--out file.html]
          solohtml studio
          solohtml version

        Shortcuts:
          solohtml file.solohtml

        Examples:
          solohtml compile examples/html/hello.solohtml
          solohtml watch examples/html/showcase.solohtml
          dotnet run --project src/SoloHtml.Studio
        """);
}
