namespace SoloRust.Compiler;

public sealed class SoloRustProgram
{
    public List<SoloRustNode> Items { get; } = [];
}

public abstract class SoloRustNode
{
    public int Line { get; init; }
}

public sealed class SoloRustFn : SoloRustNode
{
    public required string Name { get; init; }
    public List<(string Name, string Type)> Params { get; } = [];
    public string? ReturnType { get; init; }
    public List<SoloRustStmt> Body { get; } = [];
}

public abstract class SoloRustStmt
{
    public int Line { get; init; }
}

public sealed class SoloRustLet : SoloRustStmt
{
    public bool Mutable { get; init; }
    public required string Name { get; init; }
    public string? Type { get; init; }
    public required string Value { get; init; }
}

public sealed class SoloRustPrintln : SoloRustStmt
{
    public required string Template { get; init; }
    public List<string> Args { get; init; } = [];
}

public sealed class SoloRustIf : SoloRustStmt
{
    public required string Condition { get; init; }
    public List<SoloRustStmt> ThenBody { get; } = [];
    public List<SoloRustStmt> ElseBody { get; } = [];
}

public sealed class SoloRustFor : SoloRustStmt
{
    public required string Variable { get; init; }
    public required string RangeStart { get; init; }
    public required string RangeEnd { get; init; }
    public List<SoloRustStmt> Body { get; } = [];
}

public sealed class SoloRustWhile : SoloRustStmt
{
    public required string Condition { get; init; }
    public List<SoloRustStmt> Body { get; } = [];
}

public sealed class SoloRustReturn : SoloRustStmt
{
    public string? Value { get; init; }
}

public sealed class SoloRustExprStmt : SoloRustStmt
{
    public required string Code { get; init; }
}

public sealed class SoloRustComment : SoloRustStmt
{
    public required string Text { get; init; }
}
