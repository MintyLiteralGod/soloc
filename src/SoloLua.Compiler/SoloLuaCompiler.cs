namespace SoloLua.Compiler;

public static class SoloLuaCompiler
{
    public static CompileResult Compile(string source, string? title = null)
    {
        try
        {
            var program = new SoloLuaParser().Parse(source);
            var lua = new LuaEmitter().Emit(program, title);
            return new CompileResult(true, lua, Array.Empty<string>(), Notes);
        }
        catch (SoloLuaException ex)
        {
            return new CompileResult(false, string.Empty, [ex.Message], Array.Empty<string>());
        }
        catch (Exception ex)
        {
            return new CompileResult(false, string.Empty, [$"SoloLua error: {ex.Message}"], Array.Empty<string>());
        }
    }

    private static readonly string[] Notes =
    [
        "Locals by default — use `global name = …` for _G.",
        "`!=` `&&` `||` `!` rewrite to Lua operators; `continue` uses goto labels.",
        "`list` refuses nil slots (no array holes). Prefer `at0`/`set0` for 0-based indexing.",
        "Target: Lua 5.4 / LuaJIT-friendly source.",
    ];
}

public sealed record CompileResult(
    bool Ok,
    string Lua,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Notes);

public sealed class SoloLuaException : Exception
{
    public SoloLuaException(string message) : base(message) { }
}
