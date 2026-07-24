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
        "build" or "b" => Build(args),
        "new" or "n" => NewProject(args),
        "version" or "--version" or "-v" => PrintVersion(),
        "studio" or "hub" => PrintHubHint(),
        _ when Directory.Exists(args[0]) => Build(["build", .. args]),
        _ => Unknown(args[0]),
    };
}

static int Build(string[] args)
{
    string? dir = null;
    string? outPath = null;
    for (var i = 1; i < args.Length; i++)
    {
        if (args[i] is "--out" or "-o" && i + 1 < args.Length) outPath = args[++i];
        else if (!args[i].StartsWith('-')) dir = args[i];
    }

    dir ??= ".";
    var result = SoloPageCompiler.Build(dir);
    if (!result.Ok)
    {
        foreach (var e in result.Errors)
            Console.Error.WriteLine(e);
        return 1;
    }

    outPath ??= Path.Combine(Path.GetFullPath(dir), "index.html");
    File.WriteAllText(outPath, result.Html);
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Wrote {outPath}");
    if (result.HtmlPath is not null) Console.WriteLine($"  html: {result.HtmlPath}");
    if (result.CssPath is not null) Console.WriteLine($"  css:  {result.CssPath}");
    if (result.JsPath is not null) Console.WriteLine($"  js:   {result.JsPath}");
    return 0;
}

static int NewProject(string[] args)
{
    var name = args.ElementAtOrDefault(1) ?? "mysite";
    var dir = Path.GetFullPath(name);
    Directory.CreateDirectory(dir);
    Directory.CreateDirectory(Path.Combine(dir, "components"));

    File.WriteAllText(Path.Combine(dir, "page.solohtml"),
        """
        page SoloPage
          title Hello SoloPage
          css href=styles.css
          hero
            brand SoloGem
            h1 Hello, SoloPage
            p SoloHTML + SoloCSS + SoloJS in one folder.
            button primary href=#go Get started
          include components/footer.solohtml
          js src=app.js
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
        SoloPage — bundle SoloHTML + SoloCSS + SoloJS (Solo5)

        Usage:
          solopage new <name>
          solopage build [folder] [--out index.html]
          solopage hub
          solopage version

        A SoloPage folder usually contains:
          page.solohtml
          styles.solocss
          app.solojs
        """);
}
