namespace SoloCss.Compiler;

public static class SoloCssCompiler
{
    public static CompileResult Compile(string source)
    {
        try
        {
            var document = new SoloCssParser().Parse(source);
            var css = new CssEmitter().Emit(document);
            return new CompileResult(true, css, Array.Empty<string>());
        }
        catch (SoloCssException ex)
        {
            return new CompileResult(false, string.Empty, [ex.Message]);
        }
        catch (Exception ex)
        {
            return new CompileResult(false, string.Empty, [$"SoloCSS error: {ex.Message}"]);
        }
    }
}

public sealed record CompileResult(bool Ok, string Css, IReadOnlyList<string> Errors);

public sealed class SoloCssException : Exception
{
    public SoloCssException(string message) : base(message) { }
}
