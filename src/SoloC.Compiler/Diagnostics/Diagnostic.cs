using SoloC.Compiler.Text;

namespace SoloC.Compiler.Diagnostics;

public sealed class Diagnostic
{
    public Diagnostic(
        DiagnosticSeverity severity,
        string message,
        TextSpan span,
        TextLocation? location = null,
        string? tip = null)
    {
        Severity = severity;
        Message = message;
        Span = span;
        Location = location;
        Tip = tip;
    }

    public DiagnosticSeverity Severity { get; }
    public string Message { get; }
    public TextSpan Span { get; }
    public TextLocation? Location { get; }
    public string? Tip { get; }

    public override string ToString()
    {
        var where = Location is { } loc ? loc.ToString() : Span.ToString();
        var tip = Tip is null ? string.Empty : $" Tip: {Tip}";
        return $"{Severity} ({where}): {Message}.{tip}";
    }
}

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

public readonly record struct TextSpan(int Start, int Length)
{
    public int End => Start + Length;

    public override string ToString() => $"{Start}..{End}";
}
