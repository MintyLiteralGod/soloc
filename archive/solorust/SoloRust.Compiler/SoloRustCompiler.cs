namespace SoloRust.Compiler;

public static class SoloRustCompiler
{
    public static CompileResult Compile(string source, string? crateName = null)
    {
        try
        {
            var program = new SoloRustParser().Parse(source);
            var rust = new RustEmitter().Emit(program, crateName, borrowCoach: true);
            return new CompileResult(true, rust, Array.Empty<string>(), Notes);
        }
        catch (SoloRustException ex)
        {
            return new CompileResult(false, string.Empty, [ex.Message], Notes);
        }
        catch (Exception ex)
        {
            return new CompileResult(false, string.Empty, [$"SoloRUST error: {ex.Message}"], Notes);
        }
    }

    private static readonly string[] Notes =
    [
        "SoloRUST is experimental (Solo5 research track).",
        "Output is beginner-friendly Rust source — compile further with `rustc` or `cargo`.",
        "Ownership, lifetimes, and traits are intentionally simplified or deferred.",
    ];
}

public sealed record CompileResult(
    bool Ok,
    string Rust,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Notes);

public sealed class SoloRustException : Exception
{
    public SoloRustException(string message) : base(message) { }
}
