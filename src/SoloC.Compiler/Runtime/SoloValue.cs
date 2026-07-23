using System.Globalization;

namespace SoloC.Compiler.Runtime;

public readonly struct SoloValue : IEquatable<SoloValue>
{
    public static SoloValue Null { get; } = new(SoloValueKind.Null, null);
    public static SoloValue Void { get; } = new(SoloValueKind.Void, null);

    private SoloValue(SoloValueKind kind, object? raw)
    {
        Kind = kind;
        Raw = raw;
    }

    public SoloValueKind Kind { get; }
    public object? Raw { get; }

    public static SoloValue FromInt(int value) => new(SoloValueKind.Int, value);
    public static SoloValue FromDouble(double value) => new(SoloValueKind.Double, value);
    public static SoloValue FromBool(bool value) => new(SoloValueKind.Bool, value);
    public static SoloValue FromString(string value) => new(SoloValueKind.String, value);
    public static SoloValue FromObject(SoloObject value) => new(SoloValueKind.Object, value);
    public static SoloValue FromFunction(SoloFunction value) => new(SoloValueKind.Function, value);
    public static SoloValue FromNative(NativeFunction value) => new(SoloValueKind.NativeFunction, value);

    public static SoloValue FromClr(object? value) => value switch
    {
        null => Null,
        int i => FromInt(i),
        double d => FromDouble(d),
        float f => FromDouble(f),
        bool b => FromBool(b),
        string s => FromString(s),
        SoloObject o => FromObject(o),
        SoloFunction fn => FromFunction(fn),
        NativeFunction n => FromNative(n),
        SoloValue v => v,
        _ => throw new InvalidOperationException($"Unsupported CLR value type '{value.GetType().Name}'."),
    };

    public int AsInt() => Kind == SoloValueKind.Int
        ? (int)Raw!
        : throw new InvalidOperationException($"Expected int but got {Kind}.");

    public double AsDouble() => Kind switch
    {
        SoloValueKind.Double => (double)Raw!,
        SoloValueKind.Int => (int)Raw!,
        _ => throw new InvalidOperationException($"Expected number but got {Kind}."),
    };

    public bool AsBool() => Kind == SoloValueKind.Bool
        ? (bool)Raw!
        : throw new InvalidOperationException($"Expected bool but got {Kind}.");

    public string AsString() => Kind == SoloValueKind.String
        ? (string)Raw!
        : throw new InvalidOperationException($"Expected string but got {Kind}.");

    public SoloObject AsObject() => Kind == SoloValueKind.Object
        ? (SoloObject)Raw!
        : throw new InvalidOperationException($"Expected object but got {Kind}.");

    public bool IsTruthy() => Kind switch
    {
        SoloValueKind.Null or SoloValueKind.Void => false,
        SoloValueKind.Bool => (bool)Raw!,
        SoloValueKind.Int => (int)Raw! != 0,
        SoloValueKind.Double => (double)Raw! != 0,
        SoloValueKind.String => !string.IsNullOrEmpty((string)Raw!),
        _ => true,
    };

    public override string ToString() => Kind switch
    {
        SoloValueKind.Null => "null",
        SoloValueKind.Void => "",
        SoloValueKind.Int => ((int)Raw!).ToString(CultureInfo.InvariantCulture),
        SoloValueKind.Double => ((double)Raw!).ToString(CultureInfo.InvariantCulture),
        SoloValueKind.Bool => (bool)Raw! ? "true" : "false",
        SoloValueKind.String => (string)Raw!,
        SoloValueKind.Object => ((SoloObject)Raw!).ToString(),
        SoloValueKind.Function => $"<fn {((SoloFunction)Raw!).Name}>",
        SoloValueKind.NativeFunction => $"<native {((NativeFunction)Raw!).Name}>",
        _ => Raw?.ToString() ?? "null",
    };

    public bool Equals(SoloValue other) => Kind == other.Kind && Equals(Raw, other.Raw);
    public override bool Equals(object? obj) => obj is SoloValue other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Kind, Raw);
}

public enum SoloValueKind
{
    Null,
    Void,
    Int,
    Double,
    Bool,
    String,
    Object,
    Function,
    NativeFunction,
}

public sealed class SoloObject
{
    public SoloObject(SoloClassDefinition definition)
    {
        Definition = definition;
        Fields = new Dictionary<string, SoloValue>(StringComparer.Ordinal);
        foreach (var field in definition.Fields)
            Fields[field.Name] = field.DefaultValue;
    }

    public SoloClassDefinition Definition { get; }
    public Dictionary<string, SoloValue> Fields { get; }

    public override string ToString() => $"<{Definition.Name} object>";
}

public sealed class SoloClassDefinition
{
    public SoloClassDefinition(
        string name,
        IReadOnlyList<SoloFieldDefinition> fields,
        IReadOnlyDictionary<string, SoloFunction> methods)
    {
        Name = name;
        Fields = fields;
        Methods = methods;
    }

    public string Name { get; }
    public IReadOnlyList<SoloFieldDefinition> Fields { get; }
    public IReadOnlyDictionary<string, SoloFunction> Methods { get; }
}

public sealed class SoloFieldDefinition
{
    public SoloFieldDefinition(string name, SoloValue defaultValue)
    {
        Name = name;
        DefaultValue = defaultValue;
    }

    public string Name { get; }
    public SoloValue DefaultValue { get; }
}

public sealed class SoloFunction
{
    public SoloFunction(
        string name,
        IReadOnlyList<string> parameters,
        Syntax.BlockStatementSyntax body,
        SoloEnvironment? closure,
        bool isMethod = false)
    {
        Name = name;
        Parameters = parameters;
        Body = body;
        Closure = closure;
        IsMethod = isMethod;
    }

    public string Name { get; }
    public IReadOnlyList<string> Parameters { get; }
    public Syntax.BlockStatementSyntax Body { get; }
    public SoloEnvironment? Closure { get; }
    public bool IsMethod { get; }
}

public sealed class NativeFunction
{
    public NativeFunction(string name, Func<IReadOnlyList<SoloValue>, SoloValue> implementation)
    {
        Name = name;
        Implementation = implementation;
    }

    public string Name { get; }
    public Func<IReadOnlyList<SoloValue>, SoloValue> Implementation { get; }
}
