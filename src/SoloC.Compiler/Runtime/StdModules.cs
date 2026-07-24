namespace SoloC.Compiler.Runtime;

/// <summary>
/// Built-in modules imported with <c>using Name;</c>.
/// </summary>
public static class StdModules
{
    public static readonly IReadOnlyCollection<string> Names = ["Math", "Console"];

    public static bool TryImport(string name, SoloEnvironment globals, out string? error)
    {
        error = null;
        switch (name)
        {
            case "Math":
                ImportMath(globals);
                return true;
            case "Console":
                if (!globals.TryGet("Console", out _))
                {
                    var console = new SoloClassDefinition(
                        "Console",
                        [],
                        new Dictionary<string, SoloFunction>(StringComparer.Ordinal));
                    globals.Define("Console", SoloValue.FromObject(new SoloObject(console)));
                }

                return true;
            default:
                error = $"I don't know a module named '{name}'. Try: {string.Join(", ", Names)}.";
                return false;
        }
    }

    public static bool TryImport(string name, IDictionary<string, SoloValue> globals, out string? error)
    {
        var env = new SoloEnvironment();
        foreach (var (key, value) in globals)
            env.Define(key, value);

        if (!TryImport(name, env, out error))
            return false;

        foreach (var moduleName in new[] { "abs", "min", "max", "sqrt", "pow", "Console" })
        {
            if (env.TryGet(moduleName, out var value))
                globals[moduleName] = value;
        }

        return true;
    }

    private static void ImportMath(SoloEnvironment globals)
    {
        DefineIfMissing(globals, "abs", args =>
        {
            RequireCount("abs", args, 1);
            return args[0].Kind == SoloValueKind.Int
                ? SoloValue.FromInt(Math.Abs(args[0].AsInt()))
                : SoloValue.FromDouble(Math.Abs(args[0].AsDouble()));
        });

        DefineIfMissing(globals, "min", args =>
        {
            RequireCount("min", args, 2);
            if (args[0].Kind == SoloValueKind.Int && args[1].Kind == SoloValueKind.Int)
                return SoloValue.FromInt(Math.Min(args[0].AsInt(), args[1].AsInt()));
            return SoloValue.FromDouble(Math.Min(args[0].AsDouble(), args[1].AsDouble()));
        });

        DefineIfMissing(globals, "max", args =>
        {
            RequireCount("max", args, 2);
            if (args[0].Kind == SoloValueKind.Int && args[1].Kind == SoloValueKind.Int)
                return SoloValue.FromInt(Math.Max(args[0].AsInt(), args[1].AsInt()));
            return SoloValue.FromDouble(Math.Max(args[0].AsDouble(), args[1].AsDouble()));
        });

        DefineIfMissing(globals, "sqrt", args =>
        {
            RequireCount("sqrt", args, 1);
            return SoloValue.FromDouble(Math.Sqrt(args[0].AsDouble()));
        });

        DefineIfMissing(globals, "pow", args =>
        {
            RequireCount("pow", args, 2);
            return SoloValue.FromDouble(Math.Pow(args[0].AsDouble(), args[1].AsDouble()));
        });
    }

    private static void DefineIfMissing(SoloEnvironment globals, string name, Func<IReadOnlyList<SoloValue>, SoloValue> impl)
    {
        if (globals.TryGet(name, out _))
            return;

        globals.Define(name, SoloValue.FromNative(new NativeFunction(name, impl)));
    }

    private static void RequireCount(string name, IReadOnlyList<SoloValue> args, int count)
    {
        if (args.Count != count)
            throw new RuntimeException($"'{name}' expects {count} argument(s), but got {args.Count}.");
    }
}
