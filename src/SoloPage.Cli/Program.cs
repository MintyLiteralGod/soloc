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
    string? baseUrl = null;
    var forceInline = false;
    for (var i = 1; i < args.Length; i++)
    {
        if (args[i] is "--out" or "-o" && i + 1 < args.Length) outPath = args[++i];
        else if (args[i] is "--base-url" && i + 1 < args.Length) baseUrl = args[++i];
        else if (args[i] is "--inline") forceInline = true;
        else if (!args[i].StartsWith('-')) dir = args[i];
    }

    dir ??= ".";
    var projectDir = Path.GetFullPath(dir);

    if (!watch)
        return BuildOnce(projectDir, outPath, forceInline, baseUrl);

    Console.WriteLine($"Watching {projectDir} — rebuild on Solo sources / data / public");
    Console.WriteLine("Press Ctrl+C to stop.");

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    BuildOnce(projectDir, outPath, forceInline, baseUrl);

    using var watcher = new FileSystemWatcher(projectDir)
    {
        IncludeSubdirectories = true,
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        Filter = "*.*",
    };

    var pending = false;
    void OnChange(object _, FileSystemEventArgs e)
    {
        if (!IsWatchedSource(e.FullPath))
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
                Thread.Sleep(80);
                if (pending)
                    continue;
                BuildOnce(projectDir, outPath, forceInline, baseUrl);
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

static int BuildOnce(string projectDir, string? outPath, bool forceInline, string? baseUrl)
{
    var result = SoloPageCompiler.Build(projectDir, new SoloPageOptions
    {
        ForceInline = forceInline,
        BaseUrl = baseUrl,
    });
    if (!result.Ok)
    {
        foreach (var e in result.Errors)
            Console.Error.WriteLine(e);
        return 1;
    }

    var files = result.Files ?? [new SoloPageArtifact("index.html", result.Html)];
    var outDir = ResolveOutDir(projectDir, outPath, result.IsSite || files.Count > 1);

    foreach (var file in files)
    {
        var dest = Path.Combine(outDir, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.WriteAllText(dest, file.Content);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Wrote {dest}");
    }

    var publicDir = Path.Combine(projectDir, "public");
    if (Directory.Exists(publicDir))
    {
        foreach (var src in Directory.EnumerateFiles(publicDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(publicDir, src);
            var dest = Path.Combine(outDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(src, dest, overwrite: true);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Copied public/{rel.Replace('\\', '/')}");
        }
    }

    if (result.HtmlPath is not null) Console.WriteLine($"  html: {result.HtmlPath}");
    if (result.CssPath is not null) Console.WriteLine($"  css:  {result.CssPath}");
    if (result.JsPath is not null) Console.WriteLine($"  js:   {result.JsPath}");
    if (result.IsSite) Console.WriteLine($"  site: {files.Count(f => f.RelativePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))} routes");
    return 0;
}

static string ResolveOutDir(string projectDir, string? outPath, bool multi)
{
    if (string.IsNullOrWhiteSpace(outPath))
        return multi ? Path.Combine(projectDir, "dist") : projectDir;

    var full = Path.GetFullPath(outPath);
    if (multi || Directory.Exists(full) || !full.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
    {
        Directory.CreateDirectory(full);
        return full;
    }

    // Single-file --out index.html → write into that file's directory as index.html only
    return Path.GetDirectoryName(full) ?? projectDir;
}

static bool IsWatchedSource(string path)
{
    var name = Path.GetFileName(path);
    if (name is "site.json" or "site.solo5.json" or "solopage.json")
        return true;
    var ext = Path.GetExtension(path);
    return ext.Equals(".solohtml", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".solocss", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".solojs", StringComparison.OrdinalIgnoreCase)
        || ext.Equals(".json", StringComparison.OrdinalIgnoreCase)
        || path.Contains($"{Path.DirectorySeparatorChar}public{Path.DirectorySeparatorChar}");
}

static int NewProject(string[] args)
{
    var name = args.ElementAtOrDefault(1) ?? "mysite";
    var site = args.Any(a => a is "--site" or "--multi");
    var dir = Path.GetFullPath(name);
    Directory.CreateDirectory(dir);

    if (site)
    {
        Directory.CreateDirectory(Path.Combine(dir, "pages"));
        Directory.CreateDirectory(Path.Combine(dir, "layouts"));
        Directory.CreateDirectory(Path.Combine(dir, "components"));
        Directory.CreateDirectory(Path.Combine(dir, "tokens"));

        File.WriteAllText(Path.Combine(dir, "layouts", "shell.solohtml"),
            """
            page theme=none
              head
                favicon href=/favicon.svg
                meta name=description content=SoloPage site
                og title=SoloPage
                og description=Multi-page Solo5 site
              include ../components/nav.solohtml
              main
                slot
              include ../components/footer.solohtml
            """);

        File.WriteAllText(Path.Combine(dir, "components", "nav.solohtml"),
            """
            nav #site-nav
              a href=/ Home
              a href=/deskcore DeskCore
              button #menu-btn type=button Menu
            """);

        File.WriteAllText(Path.Combine(dir, "components", "footer.solohtml"),
            """
            footer
              p Made with Solo5 · SoloPage
            """);

        File.WriteAllText(Path.Combine(dir, "pages", "index.solohtml"),
            """
            layout ../layouts/shell.solohtml
              title Home
              hero
                brand SoloGem
                h1 Multi-page SoloPage
                p Shared layout, real head tags, SoloCSS tokens.
                a.btn.primary href=/deskcore Open DeskCore
            """);

        File.WriteAllText(Path.Combine(dir, "pages", "deskcore.solohtml"),
            """
            layout ../layouts/shell.solohtml
              title DeskCore
              section
                h1 DeskCore
                p Second route — same shell, different body.
                form action=mailto:hello@example.com method=post
                  label Email
                  input type=email name=email required=true
                  button.btn.primary type=submit Send
            """);

        File.WriteAllText(Path.Combine(dir, "tokens", "brand.solocss"),
            """
            vars
              brand #0f2a22
              accent #d8ff3e
              paper #f4fff8
            """);

        File.WriteAllText(Path.Combine(dir, "styles.solocss"),
            """
            include tokens/brand.solocss

            body
              margin 0
              font "Segoe UI", system-ui, sans-serif
              background $paper
              color $brand

            nav
              display flex
              gap 1rem
              pad 1rem 1.5rem

            nav a.active
              bold

            .hero
              padding 4rem
              background linear-gradient(145deg, $brand, $accent)
              color $paper

            .btn
              display inline-block
              pad 0.75rem 1.2rem
              background $accent
              color $brand
              radius 0.55rem
              no-underline
              bold
            """);

        File.WriteAllText(Path.Combine(dir, "app.solojs"),
            """
            when ready
              solo.route.markActive("nav a")
              on click "#menu-btn"
                toggleClass "#site-nav" open
                set "#menu-btn" attr aria-expanded true
            """);

        Console.WriteLine($"Created SoloPage site: {dir}");
        Console.WriteLine("Next:");
        Console.WriteLine($"  solopage build {name}");
        Console.WriteLine($"  # → {name}/dist/index.html + dist/deskcore/index.html + assets/");
        return 0;
    }

    Directory.CreateDirectory(Path.Combine(dir, "components"));

    File.WriteAllText(Path.Combine(dir, "page.solohtml"),
        """
        page SoloPage theme=none
          title Hello SoloPage
          hero
            brand SoloGem
            h1 Hello, SoloPage
            p SoloHTML + SoloCSS + SoloJS in one folder.
            a.btn.primary href=#go Get started
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

        .btn
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
    Console.WriteLine($"  solopage new {name}-site --site   # multi-page");
    return 0;
}

static int PrintVersion()
{
    Console.WriteLine("SoloPage 0.2.0 — SoloGem Solo5 (MIT)");
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

        Usage:
          solopage new <name> [--site]
          solopage build [folder] [--out dist|index.html] [--inline]
          solopage watch [folder]
          solopage hub
          solopage version

        Single page:
          page.solohtml + styles.solocss + app.solojs → index.html

        Site mode (pages/ folder):
          pages/index.solohtml     → dist/index.html        (/)
          pages/deskcore.solohtml  → dist/deskcore/index.html (/deskcore/)
          + shared assets/site.css + assets/site.js

        Layouts: `layout layouts/shell.solohtml` with a `slot` in the shell.
        Head: favicon, og, canonical, link, meta — `link` is HTML <link>, use `a` for anchors.
        """);
}
