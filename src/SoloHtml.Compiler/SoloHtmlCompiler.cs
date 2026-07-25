namespace SoloHtml.Compiler;

public static class SoloHtmlCompiler
{
    public static CompileResult Compile(
        string source,
        string? pageTitle = null,
        string? basePath = null,
        SoloHtmlEmitOptions? emitOptions = null)
    {
        try
        {
            var document = new SoloHtmlParser().Parse(source);
            document = SoloHtmlIncludeExpander.Expand(document, basePath);
            document = SoloHtmlLayoutExpander.Expand(document, basePath);
            var emitter = new HtmlEmitter(
                includeDefaultTheme: emitOptions?.IncludeDefaultTheme,
                forceDefaultTheme: emitOptions?.ForceDefaultTheme ?? false);
            var html = emitter.Emit(document, pageTitle);
            return new CompileResult(true, html, Array.Empty<string>());
        }
        catch (SoloHtmlException ex)
        {
            return new CompileResult(false, string.Empty, [ex.Message]);
        }
        catch (Exception ex)
        {
            return new CompileResult(false, string.Empty, [$"SoloHTML error: {ex.Message}"]);
        }
    }

    public static CompileResult CompileFile(string path, string? pageTitle = null, SoloHtmlEmitOptions? emitOptions = null)
    {
        var source = File.ReadAllText(path);
        var basePath = Path.GetDirectoryName(Path.GetFullPath(path))!;
        return Compile(source, pageTitle, basePath, emitOptions);
    }
}

/// <summary>
/// Emission knobs. Default theme CSS is skipped when the page opts out
/// (<c>theme=none</c>, <c>notheme</c>, <c>bare</c>), links a stylesheet, or the caller sets
/// <see cref="IncludeDefaultTheme"/> to false (e.g. SoloPage with a <c>.solocss</c> file).
/// </summary>
public sealed class SoloHtmlEmitOptions
{
    public bool? IncludeDefaultTheme { get; set; }
    public bool ForceDefaultTheme { get; set; }
}

public sealed record CompileResult(bool Ok, string Html, IReadOnlyList<string> Errors);
