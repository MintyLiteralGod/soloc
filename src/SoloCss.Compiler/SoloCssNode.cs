namespace SoloCss.Compiler;

public sealed class SoloCssDocument
{
    public Dictionary<string, string> Vars { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<SoloCssBlock> Blocks { get; } = [];
}

public abstract class SoloCssBlock
{
    public int Line { get; init; }
}

public sealed class SoloCssRule : SoloCssBlock
{
    public required string Selector { get; init; }
    public List<(string Property, string Value)> Declarations { get; } = [];
    public List<SoloCssRule> Children { get; } = [];
}

public sealed class SoloCssMedia : SoloCssBlock
{
    public required string Query { get; init; }
    public List<SoloCssRule> Rules { get; } = [];
}
