using SoloCss.Compiler;
using SoloHtml.Compiler;
using SoloJs.Compiler;

namespace SoloPage.Compiler;

/// <summary>
/// Bundles SoloHTML + SoloCSS + SoloJS into one HTML page (Solo5 SoloPage).
/// </summary>
public static class SoloPageCompiler
{
    public static SoloPageResult Build(string projectDir, SoloPageOptions? options = null)
    {
        options ??= new SoloPageOptions();
        projectDir = Path.GetFullPath(projectDir);
        if (!Directory.Exists(projectDir))
            return SoloPageResult.Fail([$"Project folder not found: {projectDir}"]);

        var htmlPath = FindFirst(projectDir, options.HtmlName, "*.solohtml");
        var cssPath = FindFirst(projectDir, options.CssName, "*.solocss");
        var jsPath = FindFirst(projectDir, options.JsName, "*.solojs");

        if (htmlPath is null)
            return SoloPageResult.Fail(["SoloPage needs a .solohtml file (e.g. page.solohtml)."]);

        var htmlSource = File.ReadAllText(htmlPath);
        var htmlResult = SoloHtmlCompiler.Compile(htmlSource, options.Title, Path.GetDirectoryName(htmlPath));
        if (!htmlResult.Ok)
            return SoloPageResult.Fail(htmlResult.Errors.Select(e => $"SoloHTML: {e}").ToArray());

        string? css = null;
        if (cssPath is not null)
        {
            var cssResult = SoloCssCompiler.Compile(File.ReadAllText(cssPath));
            if (!cssResult.Ok)
                return SoloPageResult.Fail(cssResult.Errors.Select(e => $"SoloCSS: {e}").ToArray());
            css = cssResult.Css;
        }

        string? js = null;
        var usesReact = false;
        if (jsPath is not null)
        {
            var jsResult = SoloJsCompiler.Compile(File.ReadAllText(jsPath), Path.GetFileNameWithoutExtension(jsPath));
            if (!jsResult.Ok)
                return SoloPageResult.Fail(jsResult.Errors.Select(e => $"SoloJS: {e}").ToArray());
            js = jsResult.JavaScript;
            usesReact = jsResult.UsesReact;
        }

        var bundled = Bundle(htmlResult.Html, css, js, options.InlineAssets, usesReact || options.UseReact);
        return new SoloPageResult(true, bundled, Array.Empty<string>(), htmlPath, cssPath, jsPath, usesReact || options.UseReact);
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

    private static string Bundle(string html, string? css, string? js, bool inline, bool useReact)
    {
        if (!inline)
            return html;

        var result = html;

        if (useReact)
        {
            var reactScripts =
                """
                <script crossorigin src="https://unpkg.com/react@18.3.1/umd/react.development.js"></script>
                <script crossorigin src="https://unpkg.com/react-dom@18.3.1/umd/react-dom.development.js"></script>
                """;
            if (result.Contains("</head>", StringComparison.OrdinalIgnoreCase))
                result = ReplaceOnce(result, "</head>", reactScripts + "\n</head>", StringComparison.OrdinalIgnoreCase);
            else
                result = reactScripts + "\n" + result;
        }

        if (!string.IsNullOrWhiteSpace(css))
        {
            var styleBlock = "<style>\n" + css + "</style>";
            if (result.Contains("</head>", StringComparison.OrdinalIgnoreCase))
                result = ReplaceOnce(result, "</head>", styleBlock + "\n</head>", StringComparison.OrdinalIgnoreCase);
            else
                result = styleBlock + "\n" + result;
        }

        if (!string.IsNullOrWhiteSpace(js))
        {
            var scriptBlock = "<script>\n" + js + "\n</script>";
            if (result.Contains("</body>", StringComparison.OrdinalIgnoreCase))
                result = ReplaceOnce(result, "</body>", scriptBlock + "\n</body>", StringComparison.OrdinalIgnoreCase);
            else
                result += "\n" + scriptBlock;
        }

        return result;
    }

    private static string ReplaceOnce(string input, string oldValue, string newValue, StringComparison comparison)
    {
        var idx = input.IndexOf(oldValue, comparison);
        if (idx < 0)
            return input;
        return input[..idx] + newValue + input[(idx + oldValue.Length)..];
    }
}

public sealed class SoloPageOptions
{
    public string? Title { get; set; }
    public string? HtmlName { get; set; } = "page.solohtml";
    public string? CssName { get; set; } = "styles.solocss";
    public string? JsName { get; set; } = "app.solojs";
    public bool InlineAssets { get; set; } = true;
    public bool UseReact { get; set; }
}

public sealed record SoloPageResult(
    bool Ok,
    string Html,
    IReadOnlyList<string> Errors,
    string? HtmlPath = null,
    string? CssPath = null,
    string? JsPath = null,
    bool UsesReact = false)
{
    public static SoloPageResult Fail(IReadOnlyList<string> errors) =>
        new(false, string.Empty, errors);
}
