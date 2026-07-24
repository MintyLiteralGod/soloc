namespace SoloJs.Compiler;

public sealed class SoloJsProgram
{
    public List<SoloJsNode> Body { get; } = [];
}

public abstract class SoloJsNode
{
    public int Line { get; init; }
}

public sealed class SoloJsComment : SoloJsNode
{
    public required string Text { get; init; }
}

public sealed class SoloJsRaw : SoloJsNode
{
    public required string Code { get; init; }
}

public sealed class SoloJsFn : SoloJsNode
{
    public required string Name { get; init; }
    public List<string> Params { get; init; } = [];
    public List<SoloJsNode> Body { get; } = [];
}

public sealed class SoloJsIf : SoloJsNode
{
    public required string Condition { get; init; }
    public List<SoloJsNode> ThenBody { get; } = [];
    public List<SoloJsNode> ElseBody { get; } = [];
}

public sealed class SoloJsWhile : SoloJsNode
{
    public required string Condition { get; init; }
    public List<SoloJsNode> Body { get; } = [];
}

public sealed class SoloJsForRange : SoloJsNode
{
    public required string Variable { get; init; }
    public required string Start { get; init; }
    public required string End { get; init; }
    public List<SoloJsNode> Body { get; } = [];
}

public sealed class SoloJsForEach : SoloJsNode
{
    public required string Variable { get; init; }
    public required string Iterable { get; init; }
    public List<SoloJsNode> Body { get; } = [];
}

public sealed class SoloJsWhenReady : SoloJsNode
{
    public List<SoloJsNode> Body { get; } = [];
}

public sealed class SoloJsOn : SoloJsNode
{
    public required string Selector { get; init; }
    public required string EventName { get; init; }
    public List<SoloJsNode> Body { get; } = [];
}

public sealed class SoloJsSet : SoloJsNode
{
    public required string Selector { get; init; }
    public required string Property { get; init; }
    public required string Value { get; init; }
}

public sealed class SoloJsFetch : SoloJsNode
{
    public required string Url { get; init; }
    public string? Into { get; init; }
    public List<SoloJsNode> ThenBody { get; } = [];
    public List<SoloJsNode> CatchBody { get; } = [];
}

public sealed class SoloJsAfter : SoloJsNode
{
    public required string DelayMs { get; init; }
    public List<SoloJsNode> Body { get; } = [];
}

public sealed class SoloJsEvery : SoloJsNode
{
    public required string IntervalMs { get; init; }
    public List<SoloJsNode> Body { get; } = [];
}

public sealed class SoloJsPrint : SoloJsNode
{
    public required string Args { get; init; }
}

public sealed class SoloJsReturn : SoloJsNode
{
    public string? Value { get; init; }
}

public sealed class SoloJsAssign : SoloJsNode
{
    public string? Keyword { get; init; } // var|let|const|null
    public required string Name { get; init; }
    public required string Value { get; init; }
}

public sealed class SoloJsExpr : SoloJsNode
{
    public required string Code { get; init; }
}
