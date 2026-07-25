using SoloPage.Compiler;

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
        "build" or "b" => Build(args, watch: false),
        "watch" or "w" => Build(args, watch: true),
        "new" or "n" => NewProject(args),
        "version" or "--version" or "-v" => PrintVersion(),
        "studio" or "hub" => PrintHubHint(),
        _ when Directory.Exists(args[0]) => Build(["build", .. args], watch: false),
        _ => Unknown(args[0]),
    };
}

static int Build(string[] args, bool watch)
{
    string? dir = null;
    string? outPath = null;
    for (var i = 1; i < args.Length; i++)
    {
        if (args[i] is "--out" or "-o" && i + 1 < args.Length) outPath = args[++i];
        else if (!args[i].StartsWith('-')) dir = args[i];
    }

    dir ??= ".";
    var projectDir = Path.GetFullPath(dir);
    outPath ??= Path.Combine(projectDir, "index.html");

    if (!watch)
        return BuildOnce(projectDir, outPath);

    Console.WriteLine($"Watching {projectDir} — rebuild on .solohtml / .solocss / .solojs save");
    Console.WriteLine("Press Ctrl+C to stop.");

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    BuildOnce(projectDir, outPath);

    using var watcher = new FileSystemWatcher(projectDir)
    {
        IncludeSubdirectories = true,
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        Filter = "*.*",
    };

    var pending = false;
    void OnChange(object _, FileSystemEventArgs e)
    {
        if (!IsSoloSource(e.FullPath) || Path.GetFullPath(e.FullPath) == Path.GetFullPath(outPath))
            return;
        pending = true;
    }

    watcher.Changed += OnChange;
    watcher.Created += OnChange;
    watcher.Renamed += OnChange;
    watcher.EnableRaisingEvents = true;

    try
    {
        while (!cts.IsCancellationRequested)
        {
            if (pending)
            {
                pending = false;
                Thread.Sleep(80); // coalesce save bursts
                if (pending)
                    continue;
                BuildOnce(projectDir, outPath);
            }

            Thread.Sleep(100);
        }
    }
    catch (OperationCanceledException)
    {
        // normal exit
    }

    return 0;
}

static int BuildOnce(string projectDir, string outPath)
{
    var result = SoloPageCompiler.Build(projectDir);
    if (!result.Ok)
    {
        foreach (var e in result.Errors)
            Console.Error.WriteLine(e);
        return 1;
    }

    File.WriteAllText(outPath, result.Html);
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Wrote {outPath}");
    if (result.HtmlPath is not null) Console.WriteLine($"  html: {result.HtmlPath}");
    if (result.CssPath is not null) Console.WriteLine($"  css:  {result.CssPath}");
    if (result.JsPath is not null) Console.WriteLine($"  js:   {result.JsPath}");
    return 0;
}

static bool IsSoloSource(string path)
{
    var ext = Path.GetExtension(path);
    return ext.Equals(".solohtml", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".solocss", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".solojs", StringComparison.OrdinalIgnoreCase);
}

static int NewProject(string[] args)
{
    var name = args.ElementAtOrDefault(1) ?? "mysite";
    var dir = Path.GetFullPath(name);
    Directory.CreateDirectory(dir);
    Directory.CreateDirectory(Path.Combine(dir, "components"));

    File.WriteAllText(Path.Combine(dir, "page.solohtml"),
        """
        page SoloPage theme=none
          title Hello SoloPage
          hero
            brand SoloGem
            h1 Hello, SoloPage
            p SoloHTML + SoloCSS + SoloJS in one folder.
            button primary href=#go Get started
          include components/footer.solohtml
        """);

    File.WriteAllText(Path.Combine(dir, "components", "footer.solohtml"),
        """
        footer
          p Made with Solo5 · SoloPage
        """);

    File.WriteAllText(Path.Combine(dir, "styles.solocss"),
        """
        vars
          brand #0f2a22
          accent #d8ff3e
          paper #f4fff8

        body
          margin 0
          font "Segoe UI", system-ui, sans-serif
          background $paper
          color $brand

        .hero
          padding 4rem
          background linear-gradient(145deg, $brand, $accent)
          color $paper

          h1
            size clamp(2.2rem, 6vw, 3.8rem)

        .button
          display inline-block
          pad 0.75rem 1.2rem
          background $accent
          color $brand
          radius 0.55rem
          no-underline
          bold

        footer
          pad 1.5rem
          center
          muted
        """);

    File.WriteAllText(Path.Combine(dir, "app.solojs"),
        """
        when ready
          print "SoloPage ready"
          after 400
            set ".hero p" text "Bundled by SoloPage — live and easy."
        """);

    Console.WriteLine($"Created SoloPage project: {dir}");
    Console.WriteLine("Next:");
    Console.WriteLine($"  solopage build {name}");
    Console.WriteLine($"  solopage watch {name}");
    return 0;
}

static int PrintVersion()
{
    Console.WriteLine("SoloPage 0.1.0 — SoloGem Solo5 (MIT)");
    return 0;
}

static int PrintHubHint()
{
    Console.WriteLine(
        """
        Solo5 Hub (all languages):
        
          dotnet run --project src/Solo5.Hub
        
        Then open http://localhost:5080
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
        SoloPage — assemble SoloHTML + SoloCSS + SoloJS (Solo5)

        A folder is the unit. Not a framework — one compile into index.html.

        Usage:
          solopage new <name>
          solopage build [folder] [--out index.html]
          solopage watch [folder] [--out index.html]
          solopage hub
          solopage version

        A SoloPage folder usually contains:
          page.solohtml
          styles.solocss
          app.solojs

        When styles.solocss is present, SoloHTML's default theme is skipped
        so your SoloCSS owns the look.

        React: if app.solojs uses component / mount, SoloPage
        injects React 18 UMD scripts automatically.

        See examples/page-react for a working SoloJS + React site.
        """);
}
