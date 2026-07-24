using SoloCss.Compiler;

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
        _ when args[0].EndsWith(".solocss", StringComparison.OrdinalIgnoreCase)
            => Compile(["compile", .. args]),
        _ => Unknown(args[0]),
    };
}

static int Compile(string[] args)
{
    var (path, outPath, stdout) = ParseCompileArgs(args);
    if (path is null) return 1;
    return CompileOnce(path, outPath, stdout);
}

static int Watch(string[] args)
{
    var (path, outPath, _) = ParseCompileArgs(args);
    if (path is null) return 1;

    outPath ??= Path.ChangeExtension(path, ".css");
    Console.WriteLine($"Watching {path} → {outPath}");
    Console.WriteLine("Press Ctrl+C to stop.");
    CompileOnce(path, outPath, stdout: false);

    using var watcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(path))!, Path.GetFileName(path))
    {
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
        EnableRaisingEvents = true,
    };

    void OnChange(object _, FileSystemEventArgs __)
    {
        Thread.Sleep(80);
        CompileOnce(path, outPath, stdout: false);
    }

    watcher.Changed += OnChange;
    watcher.Created += OnChange;
    watcher.Renamed += OnChange;

    var exit = new ManualResetEvent(false);
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; exit.Set(); };
    exit.WaitOne();
    Console.WriteLine("Stopped.");
    return 0;
}

static (string? Path, string? OutPath, bool Stdout) ParseCompileArgs(string[] args)
{
    string? path = null;
    string? outPath = null;
    var stdout = false;

    for (var i = 1; i < args.Length; i++)
    {
        if (args[i] is "--out" or "-o" && i + 1 < args.Length) outPath = args[++i];
        else if (args[i] is "--stdout") stdout = true;
        else if (!args[i].StartsWith('-')) path = args[i];
    }

    if (path is null)
    {
        Console.Error.WriteLine("Usage: solocss compile <file.solocss> [--out file.css]");
        return (null, null, false);
    }

    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"File not found: {path}");
        return (null, null, false);
    }

    return (path, outPath, stdout);
}

static int CompileOnce(string path, string? outPath, bool stdout)
{
    var result = SoloCssCompiler.Compile(File.ReadAllText(path));
    if (!result.Ok)
    {
        foreach (var error in result.Errors)
            Console.Error.WriteLine(error);
        return 1;
    }

    outPath ??= Path.ChangeExtension(path, ".css");
    if (stdout) Console.Write(result.Css);
    else
    {
        File.WriteAllText(outPath, result.Css);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Wrote {outPath}");
    }
    return 0;
}

static int PrintVersion()
{
    Console.WriteLine("SoloCSS 0.1.0 — SoloGem Solo5 (MIT)");
    return 0;
}

static int PrintStudioHint()
{
    Console.WriteLine(
        """
        SoloCSS Studio (GUI):

          dotnet run --project src/SoloCss.Studio

        Then open http://localhost:5090
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
        SoloCSS — easiest stylesheets by SoloGem (Solo5)

        Usage:
          solocss compile <file.solocss> [--out file.css] [--stdout]
          solocss watch <file.solocss>
          solocss studio
          solocss version

        Shortcuts:
          solocss file.solocss

        Docs: docs/solocss/README.md
        """);
}
