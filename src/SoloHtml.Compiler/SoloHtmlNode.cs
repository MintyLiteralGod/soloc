namespace SoloHtml.Compiler;

public sealed class SoloHtmlDocument
{
    public SoloHtmlDocument(IReadOnlyList<SoloHtmlNode> roots)
    {
        Roots = roots;
    }

    public IReadOnlyList<SoloHtmlNode> Roots { get; }
}

public sealed class SoloHtmlNode
{
    public SoloHtmlNode(
        string tag,
        string? text,
        IReadOnlyDictionary<string, string> attributes,
        IReadOnlyList<SoloHtmlNode> children,
        int line)
    {
        Tag = tag;
        Text = text;
        Attributes = attributes;
        Children = children;
        Line = line;
    }

    public string Tag { get; }
    public string? Text { get; }
    public IReadOnlyDictionary<string, string> Attributes { get; }
    public IReadOnlyList<SoloHtmlNode> Children { get; }
    public int Line { get; }
}
