namespace SoloJs.Compiler;

public static class SoloJsCompiler
{
    public static CompileResult Compile(string source, string? title = null)
    {
        try
        {
            var program = new SoloJsParser().Parse(source);
            var js = new JsEmitter().Emit(program, title);
            return new CompileResult(true, js, Array.Empty<string>(), program.UsesReact);
        }
        catch (SoloJsException ex)
        {
            return new CompileResult(false, string.Empty, [ex.Message], false);
        }
        catch (Exception ex)
        {
            return new CompileResult(false, string.Empty, [$"SoloJS error: {ex.Message}"], false);
        }
    }
}

public sealed record CompileResult(bool Ok, string JavaScript, IReadOnlyList<string> Errors, bool UsesReact = false);

public sealed class SoloJsException : Exception
{
    public SoloJsException(string message) : base(message) { }
}
