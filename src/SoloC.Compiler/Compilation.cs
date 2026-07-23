using SoloC.Compiler.Diagnostics;
using SoloC.Compiler.Runtime;
using SoloC.Compiler.Syntax;

namespace SoloC.Compiler;

public sealed class Compilation
{
    public Compilation(string source, string? fileName = null)
    {
        Source = source;
        FileName = fileName ?? "<source>";
    }

    public string Source { get; }
    public string FileName { get; }

    public static Compilation FromFile(string path)
    {
        var source = File.ReadAllText(path);
        return new Compilation(source, path);
    }

    public ParseResult Parse()
    {
        var diagnostics = new DiagnosticBag();
        var parser = new Parser(Source, diagnostics);
        var tree = parser.ParseCompilationUnit();
        return new ParseResult(tree, diagnostics.Diagnostics);
    }

    public EvaluationResult Evaluate(TextWriter? output = null)
    {
        var parse = Parse();
        if (parse.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return new EvaluationResult(SoloValue.Null, parse.Diagnostics);

        var diagnostics = new DiagnosticBag();
        diagnostics.AddRange(parse.Diagnostics);
        var interpreter = new Interpreter(output, diagnostics);
        var value = interpreter.Interpret(parse.Tree);
        return new EvaluationResult(value, diagnostics.Diagnostics);
    }
}

public sealed class ParseResult
{
    public ParseResult(CompilationUnitSyntax tree, IReadOnlyList<Diagnostic> diagnostics)
    {
        Tree = tree;
        Diagnostics = diagnostics;
    }

    public CompilationUnitSyntax Tree { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public bool Success => Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error);
}

public sealed class EvaluationResult
{
    public EvaluationResult(SoloValue value, IReadOnlyList<Diagnostic> diagnostics)
    {
        Value = value;
        Diagnostics = diagnostics;
    }

    public SoloValue Value { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public bool Success => Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error);
}
