namespace SoloC.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = [];

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    public void Report(DiagnosticSeverity severity, string message, TextSpan span)
        => _diagnostics.Add(new Diagnostic(severity, message, span));

    public void Error(string message, TextSpan span)
        => Report(DiagnosticSeverity.Error, message, span);

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
        => _diagnostics.AddRange(diagnostics);
}
