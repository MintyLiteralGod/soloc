namespace SoloLua.Compiler;

public sealed class SoloLuaProgram
{
    public List<SoloLuaNode> Body { get; } = [];
}

public abstract class SoloLuaNode
{
    public int Line { get; init; }
}

public sealed class SoloLuaComment : SoloLuaNode
{
    public required string Text { get; init; }
}

public sealed class SoloLuaFn : SoloLuaNode
{
    public required string Name { get; init; }
    public List<string> Params { get; init; } = [];
    public List<SoloLuaNode> Body { get; } = [];
    public bool IsMethod { get; init; }
}

public sealed class SoloLuaClass : SoloLuaNode
{
    public required string Name { get; init; }
    public List<SoloLuaNode> Body { get; } = [];
}

public sealed class SoloLuaIf : SoloLuaNode
{
    public required string Condition { get; init; }
    public List<SoloLuaNode> ThenBody { get; } = [];
    public List<SoloLuaNode> ElseBody { get; } = [];
}

public sealed class SoloLuaWhile : SoloLuaNode
{
    public required string Condition { get; init; }
    public List<SoloLuaNode> Body { get; } = [];
}

public sealed class SoloLuaForRange : SoloLuaNode
{
    public required string Variable { get; init; }
    public required string Start { get; init; }
    public required string End { get; init; }
    public List<SoloLuaNode> Body { get; } = [];
}

public sealed class SoloLuaForEach : SoloLuaNode
{
    public string? Key { get; init; }
    public required string Value { get; init; }
    public required string Iterable { get; init; }
    public List<SoloLuaNode> Body { get; } = [];
}

public sealed class SoloLuaPrint : SoloLuaNode
{
    public required string Args { get; init; }
}

public sealed class SoloLuaReturn : SoloLuaNode
{
    public string? Value { get; init; }
}

public sealed class SoloLuaContinue : SoloLuaNode;

public sealed class SoloLuaBreak : SoloLuaNode;

public sealed class SoloLuaAssign : SoloLuaNode
{
    public bool IsGlobal { get; init; }
    public required string Name { get; init; }
    public required string Value { get; init; }
    public string? Op { get; init; } // null, +=, -=, *=, /=, ..=
}

public sealed class SoloLuaListLiteral : SoloLuaNode
{
    public required string Name { get; init; }
    public required string Items { get; init; }
    public bool IsGlobal { get; init; }
}

public sealed class SoloLuaMapLiteral : SoloLuaNode
{
    public required string Name { get; init; }
    public bool IsGlobal { get; init; }
    public List<(string Key, string Value)> Entries { get; } = [];
}

public sealed class SoloLuaTry : SoloLuaNode
{
    public List<SoloLuaNode> TryBody { get; } = [];
    public string CatchName { get; set; } = "err";
    public List<SoloLuaNode> CatchBody { get; } = [];
}

public sealed class SoloLuaImport : SoloLuaNode
{
    public required string Module { get; init; }
    public string? Alias { get; init; }
}

public sealed class SoloLuaExpr : SoloLuaNode
{
    public required string Code { get; init; }
}
