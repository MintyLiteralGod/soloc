using SoloC.Compiler.Binding;
using SoloC.Compiler.Diagnostics;
using SoloC.Compiler.Runtime;
using SoloC.Compiler.Syntax;
using SoloC.Compiler.Text;
using SoloC.Compiler.Vm;

namespace SoloC.Compiler;

public enum ExecutionEngine
{
    Auto,
    Interpreter,
    Vm,
}

public sealed class Compilation
{
    public Compilation(string source, string? fileName = null)
    {
        SourceText = SourceText.From(source, fileName ?? "<source>");
    }

    public SourceText SourceText { get; }
    public string Source => SourceText.Text;
    public string FileName => SourceText.FileName;

    public static Compilation FromFile(string path)
    {
        var source = File.ReadAllText(path);
        return new Compilation(source, path);
    }

    public ParseResult Parse()
    {
        var diagnostics = new DiagnosticBag(SourceText);
        var parser = new Parser(Source, diagnostics);
        var tree = parser.ParseCompilationUnit();
        return new ParseResult(tree, diagnostics.Diagnostics, SourceText);
    }

    public EvaluationResult Evaluate(TextWriter? output = null, ExecutionEngine engine = ExecutionEngine.Auto)
    {
        var parse = Parse();
        var diagnostics = new DiagnosticBag(SourceText);
        diagnostics.AddRange(parse.Diagnostics);

        if (parse.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return new EvaluationResult(SoloValue.Null, diagnostics.Diagnostics, engine: null);

        var typeChecker = new TypeChecker(diagnostics);
        typeChecker.Check(parse.Tree);
        if (diagnostics.HasErrors)
            return new EvaluationResult(SoloValue.Null, diagnostics.Diagnostics, engine: null);

        if (engine is ExecutionEngine.Auto or ExecutionEngine.Vm)
        {
            var vmDiagnostics = new DiagnosticBag(SourceText);
            var compiler = new BytecodeCompiler(SourceText, vmDiagnostics);
            var program = compiler.Compile(parse.Tree);
            if (program is not null && engine == ExecutionEngine.Vm)
            {
                // Strict VM mode: surface compile infos as failures if _failed path returned null already.
            }

            if (program is not null)
            {
                var vm = new VirtualMachine(output, diagnostics);
                foreach (var member in parse.Tree.Members.OfType<UsingDirectiveSyntax>())
                    vm.ImportModule(member.Name.Text);

                var value = vm.Execute(program);
                return new EvaluationResult(value, diagnostics.Diagnostics, ExecutionEngine.Vm);
            }

            if (engine == ExecutionEngine.Vm)
            {
                diagnostics.Error(
                    "This program can't run on the bytecode VM yet (classes or functions need the interpreter).",
                    parse.Tree.Span,
                    tip: "Try `soloc run --engine interpreter file.sc`, or remove classes/fn for VM mode.");
                return new EvaluationResult(SoloValue.Null, diagnostics.Diagnostics, null);
            }
        }

        var interpreter = new Interpreter(output, diagnostics);
        var interpreted = interpreter.Interpret(parse.Tree);
        return new EvaluationResult(interpreted, diagnostics.Diagnostics, ExecutionEngine.Interpreter);
    }
}

public sealed class ParseResult
{
    public ParseResult(CompilationUnitSyntax tree, IReadOnlyList<Diagnostic> diagnostics, SourceText sourceText)
    {
        Tree = tree;
        Diagnostics = diagnostics;
        SourceText = sourceText;
    }

    public CompilationUnitSyntax Tree { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public SourceText SourceText { get; }
    public bool Success => Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error);
}

public sealed class EvaluationResult
{
    public EvaluationResult(SoloValue value, IReadOnlyList<Diagnostic> diagnostics, ExecutionEngine? engine)
    {
        Value = value;
        Diagnostics = diagnostics;
        Engine = engine;
    }

    public SoloValue Value { get; }
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
    public ExecutionEngine? Engine { get; }
    public bool Success => Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error);
}
