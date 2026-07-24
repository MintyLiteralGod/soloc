namespace SoloHtml.Compiler;

public static class SoloHtmlCompiler
{
    public static CompileResult Compile(string source, string? pageTitle = null)
    {
        try
        {
            var document = new SoloHtmlParser().Parse(source);
            var html = new HtmlEmitter().Emit(document, pageTitle);
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
}

public sealed record CompileResult(bool Ok, string Html, IReadOnlyList<string> Errors);
