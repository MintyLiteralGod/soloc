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

        var cssPath = FindFirst(projectDir, options.CssName, "*.solocss");
        var jsPath = FindFirst(projectDir, options.JsName, "*.solojs");
        var pageFiles = DiscoverPages(projectDir, options);

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

        // SoloCSS present → never fight with SoloHTML DefaultCss
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

        var multi = pageFiles.Count > 1 || options.SiteMode;
        // Multi-page: shared assets for caching unless forced inline
        var inline = options.InlineAssets && !multi;
        if (options.ForceInline)
            inline = true;

        var artifacts = new List<SoloPageArtifact>();
        string? primaryHtml = null;

        if (!inline && (css is not null || js is not null))
        {
            if (css is not null)
                artifacts.Add(new SoloPageArtifact("assets/site.css", css));
            if (js is not null)
                artifacts.Add(new SoloPageArtifact("assets/site.js", js));
        }

        foreach (var page in pageFiles)
        {
            var htmlResult = SoloHtmlCompiler.Compile(
                File.ReadAllText(page.SourcePath),
                options.Title,
                Path.GetDirectoryName(page.SourcePath),
                emitOptions);
            if (!htmlResult.Ok)
                return SoloPageResult.Fail(htmlResult.Errors.Select(e => $"SoloHTML ({page.Route}): {e}").ToArray());

            string bundled;
            if (inline)
            {
                bundled = BundleInline(htmlResult.Html, css, js, usesReact);
            }
            else
            {
                var cssHref = css is not null ? AssetHref(page.OutRelativePath, "assets/site.css") : null;
                var jsHref = js is not null ? AssetHref(page.OutRelativePath, "assets/site.js") : null;
                bundled = BundleLinked(htmlResult.Html, cssHref, jsHref, usesReact);
            }

            artifacts.Add(new SoloPageArtifact(page.OutRelativePath, bundled));
            if (page.Route is "/" or "")
                primaryHtml = bundled;
        }

        primaryHtml ??= artifacts.First(a => a.RelativePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase)).Content;

        return new SoloPageResult(
            true,
            primaryHtml,
            Array.Empty<string>(),
            pageFiles[0].SourcePath,
            cssPath,
            jsPath,
            usesReact,
            artifacts,
            multi);
    }

    private static List<PageInput> DiscoverPages(string projectDir, SoloPageOptions options)
    {
        var pagesDir = Path.Combine(projectDir, "pages");
        if (Directory.Exists(pagesDir))
        {
            return Directory.EnumerateFiles(pagesDir, "*.solohtml", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .Select(f =>
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    var route = name.Equals("index", StringComparison.OrdinalIgnoreCase) ? "/" : "/" + name.ToLowerInvariant();
                    var outRel = name.Equals("index", StringComparison.OrdinalIgnoreCase)
                        ? "index.html"
                        : $"{name.ToLowerInvariant()}/index.html";
                    return new PageInput(f, route, outRel);
                })
                .ToList();
        }

        var single = FindFirst(projectDir, options.HtmlName, "*.solohtml");
        if (single is null)
            return [];

        // Ignore layout-only / component-looking names at root
        var fileName = Path.GetFileName(single);
        if (fileName.Equals("layout.solohtml", StringComparison.OrdinalIgnoreCase))
            return [];

        return [new PageInput(single, "/", "index.html")];
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

    private sealed record PageInput(string SourcePath, string Route, string OutRelativePath);
}

public sealed class SoloPageOptions
{
    public string? Title { get; set; }
    public string? HtmlName { get; set; } = "page.solohtml";
    public string? CssName { get; set; } = "styles.solocss";
    public string? JsName { get; set; } = "app.solojs";
    /// <summary>Inline CSS/JS into each HTML (default for single-page).</summary>
    public bool InlineAssets { get; set; } = true;
    /// <summary>Force inline even in multi-page mode.</summary>
    public bool ForceInline { get; set; }
    public bool UseReact { get; set; }
    /// <summary>Treat as site even with one page under pages/.</summary>
    public bool SiteMode { get; set; }
    public bool? IncludeDefaultTheme { get; set; }
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
    bool IsSite = false)
{
    public static SoloPageResult Fail(IReadOnlyList<string> errors) =>
        new(false, string.Empty, errors);
}
