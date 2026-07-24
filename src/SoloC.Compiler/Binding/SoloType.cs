namespace SoloC.Compiler.Binding;

public enum SoloTypeKind
{
    Error,
    Void,
    Null,
    Int,
    Double,
    Bool,
    String,
    Array,
    Class,
    Function,
    Any,
}

public sealed class SoloType : IEquatable<SoloType>
{
    public static SoloType Error { get; } = new(SoloTypeKind.Error, "error");
    public static SoloType Void { get; } = new(SoloTypeKind.Void, "void");
    public static SoloType Null { get; } = new(SoloTypeKind.Null, "null");
    public static SoloType Int { get; } = new(SoloTypeKind.Int, "int");
    public static SoloType Double { get; } = new(SoloTypeKind.Double, "double");
    public static SoloType Bool { get; } = new(SoloTypeKind.Bool, "bool");
    public static SoloType String { get; } = new(SoloTypeKind.String, "string");
    public static SoloType Any { get; } = new(SoloTypeKind.Any, "any");
    public static SoloType Function { get; } = new(SoloTypeKind.Function, "function");

    private SoloType(SoloTypeKind kind, string name, SoloType? elementType = null)
    {
        Kind = kind;
        Name = name;
        ElementType = elementType;
    }

    public SoloTypeKind Kind { get; }
    public string Name { get; }
    public SoloType? ElementType { get; }

    public static SoloType ArrayOf(SoloType element) =>
        new(SoloTypeKind.Array, $"{element.Name}[]", element);

    public static SoloType Class(string name) => new(SoloTypeKind.Class, name);

    public static SoloType FromTypeName(string name) => name switch
    {
        "void" => Void,
        "int" => Int,
        "double" => Double,
        "bool" => Bool,
        "string" => String,
        "var" or "let" => Any,
        _ when name.EndsWith("[]", StringComparison.Ordinal) =>
            ArrayOf(FromTypeName(name[..^2])),
        _ => Class(name),
    };

    public bool IsNumber => Kind is SoloTypeKind.Int or SoloTypeKind.Double;
    public bool IsAssignableTo(SoloType target)
    {
        if (this == Error || target == Error || target == Any || this == Any)
            return true;
        if (this == Null)
            return target.Kind is SoloTypeKind.Class or SoloTypeKind.Array or SoloTypeKind.String or SoloTypeKind.Null;
        if (Kind == SoloTypeKind.Int && target.Kind == SoloTypeKind.Double)
            return true;
        if (Kind == SoloTypeKind.Array && target.Kind == SoloTypeKind.Array)
            return ElementType!.IsAssignableTo(target.ElementType!);
        return Equals(target);
    }

    public bool Equals(SoloType? other) =>
        other is not null && Kind == other.Kind && Name == other.Name &&
        Equals(ElementType, other.ElementType);

    public override bool Equals(object? obj) => obj is SoloType t && Equals(t);
    public override int GetHashCode() => HashCode.Combine(Kind, Name, ElementType);
    public override string ToString() => Name;
}
