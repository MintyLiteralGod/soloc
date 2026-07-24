using SoloC.Compiler.Text;

namespace SoloC.Compiler.Diagnostics;

public sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _diagnostics = [];
    private readonly SourceText? _source;

    public DiagnosticBag(SourceText? source = null)
    {
        _source = source;
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;
    public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    public void Report(DiagnosticSeverity severity, string message, TextSpan span, string? tip = null)
    {
        var location = _source?.GetLocation(span);
        _diagnostics.Add(new Diagnostic(severity, message, span, location, tip));
    }

    public void Error(string message, TextSpan span, string? tip = null)
        => Report(DiagnosticSeverity.Error, message, span, tip);

    public void Warning(string message, TextSpan span, string? tip = null)
        => Report(DiagnosticSeverity.Warning, message, span, tip);

    public void Info(string message, TextSpan span, string? tip = null)
        => Report(DiagnosticSeverity.Info, message, span, tip);

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
        => _diagnostics.AddRange(diagnostics);
}
