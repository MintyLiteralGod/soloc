namespace SoloC.Compiler.Diagnostics;

public sealed class Diagnostic
{
    public Diagnostic(DiagnosticSeverity severity, string message, TextSpan span)
    {
        Severity = severity;
        Message = message;
        Span = span;
    }

    public DiagnosticSeverity Severity { get; }
    public string Message { get; }
    public TextSpan Span { get; }

    public override string ToString() => $"{Severity}: {Message} at {Span}";
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
