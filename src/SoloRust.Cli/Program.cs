using SoloRust.Compiler;

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
        "notes" => PrintNotes(),
        _ when args[0].EndsWith(".solorust", StringComparison.OrdinalIgnoreCase)
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

    outPath ??= Path.ChangeExtension(path, ".rs");
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
        Console.Error.WriteLine("Usage: solorust compile <file.solorust> [--out file.rs]");
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
    var result = SoloRustCompiler.Compile(File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
    if (!result.Ok)
    {
        foreach (var error in result.Errors)
            Console.Error.WriteLine(error);
        return 1;
    }

    outPath ??= Path.ChangeExtension(path, ".rs");
    if (stdout) Console.Write(result.Rust);
    else
    {
        File.WriteAllText(outPath, result.Rust);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Wrote {outPath}");
    }

    foreach (var note in result.Notes)
        Console.WriteLine($"note: {note}");
    return 0;
}

static int PrintVersion()
{
    Console.WriteLine("SoloRUST 0.1.0-experimental — SoloGem Solo5 (MIT)");
    return 0;
}

static int PrintNotes()
{
    Console.WriteLine(
        """
        SoloRUST research notes
        -----------------------
        Goal: make systems-language ideas approachable after SoloC / SoloJS.

        v0.1 compiles indent-friendly SoloRUST → readable Rust source.
        Then use rustc/cargo for real binaries.

        Deliberately deferred:
          - borrow checker teaching mode
          - lifetimes syntax
          - traits / generics deep dive
          - unsafe

        See docs/solorust/README.md and docs/solorust/research.md
        """);
    return 0;
}

static int PrintStudioHint()
{
    Console.WriteLine(
        """
        SoloRUST Studio (GUI):

          dotnet run --project src/SoloRust.Studio

        Then open http://localhost:5092
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
        SoloRUST — experimental Rust-friendly language by SoloGem (Solo5)

        Usage:
          solorust compile <file.solorust> [--out file.rs] [--stdout]
          solorust watch <file.solorust>
          solorust studio
          solorust notes
          solorust version

        Docs: docs/solorust/README.md
        """);
}
