using SoloCss.Compiler;
using SoloHtml.Compiler;
using SoloJs.Compiler;

namespace SoloPage.Compiler;

/// <summary>
/// Assembles SoloHTML + SoloCSS + SoloJS into a site (one page or many routes).
/// </summary>
public static class SoloPageCompiler
{
    public static SoloPageResult Build(string projectDir, SoloPageOptions? options = null)
    {
        options ??= new SoloPageOptions();
        projectDir = Path.GetFullPath(projectDir);
        if (!Directory.Exists(projectDir))
            return SoloPageResult.Fail([$"Project folder not found: {projectDir}"]);

        SiteConfig? siteConfig = null;
        try
        {
            siteConfig = SoloPageData.LoadConfig(projectDir);
        }
        catch (Exception ex)
        {
            return SoloPageResult.Fail([$"site.json: {ex.Message}"]);
        }

        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            siteConfig ??= new SiteConfig();
            siteConfig.BaseUrl = options.BaseUrl;
        }

        var cssPath = FindFirst(projectDir, options.CssName, "*.solocss");
        var jsPath = FindFirst(projectDir, options.JsName, "*.solojs");
        List<PageInput> pageFiles;
        try
        {
            pageFiles = DiscoverPages(projectDir, options, siteConfig);
        }
        catch (Exception ex)
        {
            return SoloPageResult.Fail([ex.Message]);
        }

        if (pageFiles.Count == 0)
            return SoloPageResult.Fail(["SoloPage needs a .solohtml page (page.solohtml or pages/*.solohtml)."]);

        string? css = null;
        if (cssPath is not null)
        {
            var cssResult = SoloCssCompiler.CompileFile(cssPath);
            if (!cssResult.Ok)
                return SoloPageResult.Fail(cssResult.Errors.Select(e => $"SoloCSS: {e}").ToArray());
            css = cssResult.Css;
        }

        var emitOptions = cssPath is not null || options.IncludeDefaultTheme == false
            ? new SoloHtmlEmitOptions { IncludeDefaultTheme = false }
            : options.IncludeDefaultTheme == true
                ? new SoloHtmlEmitOptions { ForceDefaultTheme = true }
                : null;

        string? js = null;
        var usesReact = options.UseReact;
        if (jsPath is not null)
        {
            var jsResult = SoloJsCompiler.Compile(File.ReadAllText(jsPath), Path.GetFileNameWithoutExtension(jsPath));
            if (!jsResult.Ok)
                return SoloPageResult.Fail(jsResult.Errors.Select(e => $"SoloJS: {e}").ToArray());
            js = jsResult.JavaScript;
            usesReact = usesReact || jsResult.UsesReact;
        }

        var multi = pageFiles.Count > 1 || options.SiteMode || Directory.Exists(Path.Combine(projectDir, "pages"));
        var inline = options.InlineAssets && !multi;
        if (options.ForceInline)
            inline = true;

        var artifacts = new List<SoloPageArtifact>();
        string? primaryHtml = null;
        var routes = new List<string>();

        if (!inline && (css is not null || js is not null))
        {
            if (css is not null)
                artifacts.Add(new SoloPageArtifact("assets/site.css", css));
            if (js is not null)
                artifacts.Add(new SoloPageArtifact("assets/site.js", js));
        }

        foreach (var page in pageFiles)
        {
            var source = page.SourceText ?? File.ReadAllText(page.SourcePath!);
            var basePath = page.BasePath ?? Path.GetDirectoryName(page.SourcePath!)!;
            var htmlResult = SoloHtmlCompiler.Compile(source, options.Title, basePath, emitOptions);
            if (!htmlResult.Ok)
                return SoloPageResult.Fail(htmlResult.Errors.Select(e => $"SoloHTML ({page.Route}): {e}").ToArray());

            string bundled;
            if (inline)
                bundled = BundleInline(htmlResult.Html, css, js, usesReact);
            else
            {
                var cssHref = css is not null ? AssetHref(page.OutRelativePath, "assets/site.css") : null;
                var jsHref = js is not null ? AssetHref(page.OutRelativePath, "assets/site.js") : null;
                bundled = BundleLinked(htmlResult.Html, cssHref, jsHref, usesReact);
            }

            // Mark current route for path-aware nav (solo.route.markActive).
            bundled = InjectBefore(bundled, "</head>",
                $"<meta name=\"solo:route\" content=\"{page.Route}\" />\n",
                StringComparison.OrdinalIgnoreCase);

            artifacts.Add(new SoloPageArtifact(page.OutRelativePath, bundled));
            routes.Add(page.Route);
            if (page.Route is "/" or "")
                primaryHtml = bundled;
        }

        primaryHtml ??= artifacts.First(a => a.RelativePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase)).Content;

        var baseUrl = siteConfig?.BaseUrl?.TrimEnd('/');
        var genRedirects = siteConfig?.GenerateRedirects ?? multi;
        var genSitemap = (siteConfig?.GenerateSitemap ?? multi) && !string.IsNullOrWhiteSpace(baseUrl);
        var genRobots = (siteConfig?.GenerateRobots ?? multi) && !string.IsNullOrWhiteSpace(baseUrl);

        if (multi && genRedirects)
            artifacts.Add(new SoloPageArtifact("_redirects", SoloPageData.BuildRedirects(routes)));
        if (genSitemap)
            artifacts.Add(new SoloPageArtifact("sitemap.xml", SoloPageData.BuildSitemap(baseUrl!, routes)));
        if (genRobots)
            artifacts.Add(new SoloPageArtifact("robots.txt", SoloPageData.BuildRobots(baseUrl!)));

        return new SoloPageResult(
            true,
            primaryHtml,
            Array.Empty<string>(),
            pageFiles[0].SourcePath,
            cssPath,
            jsPath,
            usesReact,
            artifacts,
            multi,
            routes);
    }

    private static List<PageInput> DiscoverPages(string projectDir, SoloPageOptions options, SiteConfig? siteConfig)
    {
        var pages = new List<PageInput>();
        var pagesDir = Path.Combine(projectDir, "pages");
        if (Directory.Exists(pagesDir))
        {
            // Nested pages/tips/foo.solohtml → /tips/foo/
            foreach (var f in Directory.EnumerateFiles(pagesDir, "*.solohtml", SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                var rel = Path.GetRelativePath(pagesDir, f).Replace('\\', '/');
                var noExt = Path.ChangeExtension(rel, null)!.Replace('\\', '/');
                string route;
                string outRel;
                if (noExt.Equals("index", StringComparison.OrdinalIgnoreCase) ||
                    noExt.EndsWith("/index", StringComparison.OrdinalIgnoreCase))
                {
                    var dir = noExt.Equals("index", StringComparison.OrdinalIgnoreCase)
                        ? ""
                        : noExt[..^"/index".Length];
                    route = string.IsNullOrEmpty(dir) ? "/" : "/" + dir.ToLowerInvariant();
                    outRel = string.IsNullOrEmpty(dir) ? "index.html" : $"{dir.ToLowerInvariant()}/index.html";
                }
                else
                {
                    route = "/" + noExt.ToLowerInvariant();
                    outRel = $"{noExt.ToLowerInvariant()}/index.html";
                }
                pages.Add(new PageInput(f, null, route, outRel, Path.GetDirectoryName(f)));
            }
        }
        else
        {
            var single = FindFirst(projectDir, options.HtmlName, "*.solohtml");
            if (single is not null)
            {
                var fileName = Path.GetFileName(single);
                if (!fileName.Equals("layout.solohtml", StringComparison.OrdinalIgnoreCase))
                    pages.Add(new PageInput(single, null, "/", "index.html", Path.GetDirectoryName(single)));
            }
        }

        foreach (var gen in SoloPageData.ExpandCollections(projectDir, siteConfig))
            pages.Add(new PageInput(null, gen.Source, gen.Route, gen.OutRelativePath, gen.BasePath));

        return pages;
    }

    private static string? FindFirst(string dir, string? preferred, string glob)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            var preferredPath = Path.Combine(dir, preferred);
            if (File.Exists(preferredPath))
                return preferredPath;
        }

        return Directory.EnumerateFiles(dir, glob, SearchOption.TopDirectoryOnly)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string AssetHref(string pageOutRel, string assetRel)
    {
        var depth = pageOutRel.Count(ch => ch is '/' or '\\');
        if (depth == 0)
            return assetRel.Replace('\\', '/');
        return string.Concat(Enumerable.Repeat("../", depth)) + assetRel.Replace('\\', '/');
    }

    private static string BundleInline(string html, string? css, string? js, bool useReact)
    {
        var result = html;
        if (useReact)
            result = InjectBefore(result, "</head>", ReactScripts, StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(css))
            result = InjectBefore(result, "</head>", "<style>\n" + css + "</style>\n", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(js))
            result = InjectBefore(result, "</body>", "<script>\n" + js + "\n</script>\n", StringComparison.OrdinalIgnoreCase);
        return result;
    }

    private static string BundleLinked(string html, string? cssHref, string? jsHref, bool useReact)
    {
        var result = html;
        if (useReact)
            result = InjectBefore(result, "</head>", ReactScripts, StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(cssHref))
            result = InjectBefore(result, "</head>",
                $"<link rel=\"stylesheet\" href=\"{cssHref}\" />\n", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(jsHref))
            result = InjectBefore(result, "</body>",
                $"<script src=\"{jsHref}\"></script>\n", StringComparison.OrdinalIgnoreCase);
        return result;
    }

    private static string InjectBefore(string input, string marker, string inject, StringComparison comparison)
    {
        var idx = input.IndexOf(marker, comparison);
        if (idx < 0)
            return inject + input;
        return input[..idx] + inject + input[idx..];
    }

    private const string ReactScripts =
        """
        <script crossorigin src="https://unpkg.com/react@18.3.1/umd/react.development.js"></script>
        <script crossorigin src="https://unpkg.com/react-dom@18.3.1/umd/react-dom.development.js"></script>
        """;

    private sealed record PageInput(
        string? SourcePath,
        string? SourceText,
        string Route,
        string OutRelativePath,
        string? BasePath);
}

public sealed class SoloPageOptions
{
    public string? Title { get; set; }
    public string? HtmlName { get; set; } = "page.solohtml";
    public string? CssName { get; set; } = "styles.solocss";
    public string? JsName { get; set; } = "app.solojs";
    public bool InlineAssets { get; set; } = true;
    public bool ForceInline { get; set; }
    public bool UseReact { get; set; }
    public bool SiteMode { get; set; }
    public bool? IncludeDefaultTheme { get; set; }
    public string? BaseUrl { get; set; }
}

public sealed record SoloPageArtifact(string RelativePath, string Content);

public sealed record SoloPageResult(
    bool Ok,
    string Html,
    IReadOnlyList<string> Errors,
    string? HtmlPath = null,
    string? CssPath = null,
    string? JsPath = null,
    bool UsesReact = false,
    IReadOnlyList<SoloPageArtifact>? Files = null,
    bool IsSite = false,
    IReadOnlyList<string>? Routes = null)
{
    public static SoloPageResult Fail(IReadOnlyList<string> errors) =>
        new(false, string.Empty, errors);
}
