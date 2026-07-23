using SoloC.Compiler.Diagnostics;

namespace SoloC.Compiler.Syntax;

public sealed class SyntaxToken
{
    public SyntaxToken(SyntaxKind kind, string text, object? value, TextSpan span)
    {
        Kind = kind;
        Text = text;
        Value = value;
        Span = span;
    }

    public SyntaxKind Kind { get; }
    public string Text { get; }
    public object? Value { get; }
    public TextSpan Span { get; }

    public override string ToString() => $"{Kind}('{Text}')";
}
