using SoloC.Compiler.Diagnostics;
using SoloC.Compiler.Syntax;

namespace SoloC.Compiler.Runtime;

public sealed class Interpreter
{
    private readonly DiagnosticBag _diagnostics;
    private readonly TextWriter _output;
    private readonly SoloEnvironment _globals = new();
    private SoloEnvironment _environment;

    public Interpreter(TextWriter? output = null, DiagnosticBag? diagnostics = null)
    {
        _output = output ?? Console.Out;
        _diagnostics = diagnostics ?? new DiagnosticBag();
        _environment = _globals;
        InstallBuiltins();
    }

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.Diagnostics;

    private void InstallBuiltins()
    {
        _globals.Define("print", SoloValue.FromNative(new NativeFunction("print", args =>
        {
            _output.WriteLine(string.Join(" ", args.Select(a => a.ToString())));
            return SoloValue.Void;
        })));

        // Console.WriteLine support via a Console object with WriteLine method
        var console = new SoloClassDefinition(
            "Console",
            [],
            new Dictionary<string, SoloFunction>(StringComparer.Ordinal));
        var consoleObject = new SoloObject(console);
        // Native method dispatch handled specially by name lookup below
        _globals.Define("Console", SoloValue.FromObject(consoleObject));
    }

    public SoloValue Interpret(CompilationUnitSyntax unit)
    {
        try
        {
            // First pass: modules, then classes and functions
            foreach (var member in unit.Members)
            {
                if (member is UsingDirectiveSyntax usingDirective)
                    ImportModule(usingDirective);
            }

            foreach (var member in unit.Members)
            {
                switch (member)
                {
                    case FunctionDeclarationSyntax function:
                        DefineFunction(function);
                        break;
                    case MethodDeclarationSyntax method when method.IsStatic:
                        DefineStaticMethod(method);
                        break;
                    case ClassDeclarationSyntax classDecl:
                        DefineClass(classDecl);
                        break;
                }
            }

            // Second pass: execute top-level statements and invoke Main if present
            SoloValue last = SoloValue.Void;
            var hasTopLevelStatements = false;

            foreach (var member in unit.Members)
            {
                if (member is GlobalStatementSyntax global)
                {
                    hasTopLevelStatements = true;
                    last = Execute(global.Statement);
                }
            }

            if (!hasTopLevelStatements)
            {
                if (_globals.TryGet("Main", out var main) && main.Kind == SoloValueKind.Function)
                    last = CallFunction(main.Raw as SoloFunction ?? throw new RuntimeException("Invalid Main."), []);
                else if (TryFindStaticMain(out var staticMain))
                    last = CallFunction(staticMain, []);
            }

            return last;
        }
        catch (RuntimeException ex)
        {
            _diagnostics.Error(
                ex.Message,
                ex.Span ?? new TextSpan(0, 0),
                tip: ex.Tip ?? "Re-read the line slowly — SoloC errors are meant to guide you.");
            return SoloValue.Null;
        }
        catch (ReturnException ex)
        {
            return ex.Value;
        }
    }

    private void ImportModule(UsingDirectiveSyntax usingDirective)
    {
        if (!StdModules.TryImport(usingDirective.Name.Text, _globals, out var error))
        {
            throw new RuntimeException(
                error ?? $"Unknown module '{usingDirective.Name.Text}'.",
                usingDirective.Name.Span,
                tip: $"Available modules: {string.Join(", ", StdModules.Names)}.");
        }
    }

    private bool TryFindStaticMain(out SoloFunction main)
    {
        // Look for Class.Main registered as Class_Main or nested — we register static Main as "Main"
        main = null!;
        if (_globals.TryGet("Main", out var value) && value.Kind == SoloValueKind.Function)
        {
            main = (SoloFunction)value.Raw!;
            return true;
        }

        return false;
    }

    private void DefineFunction(FunctionDeclarationSyntax function)
    {
        var parameters = function.Parameters.Parameters.Select(p => p.Identifier.Text).ToArray();
        var fn = new SoloFunction(function.Identifier.Text, parameters, function.Body, _globals);
        _globals.Define(function.Identifier.Text, SoloValue.FromFunction(fn));
    }

    private void DefineStaticMethod(MethodDeclarationSyntax method)
    {
        var parameters = method.Parameters.Parameters.Select(p => p.Identifier.Text).ToArray();
        var fn = new SoloFunction(method.Identifier.Text, parameters, method.Body, _globals);
        if (!_globals.TryGet(method.Identifier.Text, out _))
            _globals.Define(method.Identifier.Text, SoloValue.FromFunction(fn));
        else
            _globals.Assign(method.Identifier.Text, SoloValue.FromFunction(fn));
    }

    private void DefineClass(ClassDeclarationSyntax classDecl)
    {
        var fields = new List<SoloFieldDefinition>();
        var methods = new Dictionary<string, SoloFunction>(StringComparer.Ordinal);

        foreach (var member in classDecl.Members)
        {
            switch (member)
            {
                case FieldDeclarationSyntax field:
                {
                    var defaultValue = field.Initializer is null
                        ? SoloValue.Null
                        : Evaluate(field.Initializer);
                    fields.Add(new SoloFieldDefinition(field.Identifier.Text, defaultValue));
                    break;
                }
                case MethodDeclarationSyntax method:
                {
                    var parameters = method.Parameters.Parameters.Select(p => p.Identifier.Text).ToArray();
                    var fn = new SoloFunction(
                        method.Identifier.Text,
                        parameters,
                        method.Body,
                        _globals,
                        isMethod: !method.IsStatic);
                    methods[method.Identifier.Text] = fn;

                    if (method.IsStatic && method.Identifier.Text == "Main")
                    {
                        if (!_globals.TryGet("Main", out _))
                            _globals.Define("Main", SoloValue.FromFunction(fn));
                    }

                    break;
                }
            }
        }

        var definition = new SoloClassDefinition(classDecl.Identifier.Text, fields, methods);
        // Store class definition as a native constructor function
        _globals.Define(classDecl.Identifier.Text, SoloValue.FromNative(new NativeFunction(classDecl.Identifier.Text, args =>
        {
            var instance = new SoloObject(definition);
            if (definition.Methods.TryGetValue("constructor", out var ctor) ||
                definition.Methods.TryGetValue(classDecl.Identifier.Text, out ctor))
            {
                CallMethod(ctor, instance, args);
            }

            return SoloValue.FromObject(instance);
        })));
    }

    private SoloValue Execute(StatementSyntax statement)
    {
        switch (statement)
        {
            case BlockStatementSyntax block:
                return ExecuteBlock(block, new SoloEnvironment(_environment));
            case ExpressionStatementSyntax exprStmt:
                return Evaluate(exprStmt.Expression);
            case VariableDeclarationStatementSyntax variable:
                return ExecuteVariableDeclaration(variable);
            case IfStatementSyntax ifStmt:
            {
                if (Evaluate(ifStmt.Condition).IsTruthy())
                    return Execute(ifStmt.ThenStatement);
                if (ifStmt.ElseStatement is not null)
                    return Execute(ifStmt.ElseStatement);
                return SoloValue.Void;
            }
            case WhileStatementSyntax whileStmt:
            {
                SoloValue last = SoloValue.Void;
                while (Evaluate(whileStmt.Condition).IsTruthy())
                    last = Execute(whileStmt.Body);
                return last;
            }
            case ForStatementSyntax forStmt:
            {
                var previous = _environment;
                _environment = new SoloEnvironment(previous);
                try
                {
                    if (forStmt.Initializer is not null)
                        Execute(forStmt.Initializer);

                    SoloValue last = SoloValue.Void;
                    while (forStmt.Condition is null || Evaluate(forStmt.Condition).IsTruthy())
                    {
                        last = Execute(forStmt.Body);
                        if (forStmt.Increment is not null)
                            Evaluate(forStmt.Increment);
                    }

                    return last;
                }
                finally
                {
                    _environment = previous;
                }
            }
            case ReturnStatementSyntax returnStmt:
                throw new ReturnException(returnStmt.Expression is null
                    ? SoloValue.Null
                    : Evaluate(returnStmt.Expression));
            case EmptyStatementSyntax:
                return SoloValue.Void;
            default:
                throw new RuntimeException($"Unsupported statement '{statement.Kind}'.");
        }
    }

    private SoloValue ExecuteVariableDeclaration(VariableDeclarationStatementSyntax variable)
    {
        var value = variable.Initializer is null ? SoloValue.Null : Evaluate(variable.Initializer);
        _environment.Define(variable.Identifier.Text, value, variable.IsImmutable);
        return value;
    }

    private SoloValue ExecuteBlock(BlockStatementSyntax block, SoloEnvironment environment)
    {
        var previous = _environment;
        _environment = environment;
        try
        {
            SoloValue last = SoloValue.Void;
            foreach (var statement in block.Statements)
                last = Execute(statement);
            return last;
        }
        finally
        {
            _environment = previous;
        }
    }

    private SoloValue Evaluate(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                return SoloValue.FromClr(literal.Value);
            case NameExpressionSyntax name:
                return _environment.Get(name.Identifier.Text);
            case ParenthesizedExpressionSyntax paren:
                return Evaluate(paren.Expression);
            case UnaryExpressionSyntax unary:
                return EvaluateUnary(unary);
            case BinaryExpressionSyntax binary:
                return EvaluateBinary(binary);
            case AssignmentExpressionSyntax assignment:
            {
                var value = Evaluate(assignment.Expression);
                Assign(assignment.Target, value);
                return value;
            }
            case CallExpressionSyntax call:
                return EvaluateCall(call);
            case MemberAccessExpressionSyntax member:
                return EvaluateMemberAccess(member);
            case ObjectCreationExpressionSyntax creation:
            {
                var typeName = creation.TypeName.Text;
                var ctor = _environment.Get(typeName);
                var args = creation.ArgumentList.Arguments.Select(Evaluate).ToArray();
                if (ctor.Kind != SoloValueKind.NativeFunction)
                    throw new RuntimeException($"'{typeName}' is not a constructible type.", creation.Span);
                return ((NativeFunction)ctor.Raw!).Implementation(args);
            }
            case ArrayLiteralExpressionSyntax arrayLiteral:
            {
                var elements = arrayLiteral.Elements.Select(Evaluate).ToArray();
                return SoloValue.FromArray(new SoloArray(elements));
            }
            case ElementAccessExpressionSyntax elementAccess:
            {
                var target = Evaluate(elementAccess.Expression);
                var indexValue = Evaluate(elementAccess.Index);
                if (target.Kind != SoloValueKind.Array)
                    throw new RuntimeException("Only arrays can be indexed with [ ].", elementAccess.Span, "Create an array like var nums = [1, 2, 3];");
                if (indexValue.Kind != SoloValueKind.Int)
                    throw new RuntimeException("Array indexes must be int values.", elementAccess.Index.Span);
                return target.AsArray().Get(indexValue.AsInt());
            }
            default:
                throw new RuntimeException($"Unsupported expression '{expression.Kind}'.", expression.Span);
        }
    }

    private SoloValue EvaluateUnary(UnaryExpressionSyntax unary)
    {
        var operand = Evaluate(unary.Operand);
        return unary.OperatorToken.Kind switch
        {
            SyntaxKind.PlusToken => operand.Kind is SoloValueKind.Int or SoloValueKind.Double
                ? operand
                : throw new RuntimeException("Unary '+' expects a number."),
            SyntaxKind.MinusToken => operand.Kind switch
            {
                SoloValueKind.Int => SoloValue.FromInt(-operand.AsInt()),
                SoloValueKind.Double => SoloValue.FromDouble(-operand.AsDouble()),
                _ => throw new RuntimeException("Unary '-' expects a number."),
            },
            SyntaxKind.BangToken => SoloValue.FromBool(!operand.IsTruthy()),
            _ => throw new RuntimeException($"Unsupported unary operator '{unary.OperatorToken.Text}'."),
        };
    }

    private SoloValue EvaluateBinary(BinaryExpressionSyntax binary)
    {
        // Short-circuit logical operators
        if (binary.OperatorToken.Kind == SyntaxKind.AmpersandAmpersandToken)
        {
            var leftBool = Evaluate(binary.Left);
            return !leftBool.IsTruthy() ? SoloValue.FromBool(false) : SoloValue.FromBool(Evaluate(binary.Right).IsTruthy());
        }

        if (binary.OperatorToken.Kind == SyntaxKind.PipePipeToken)
        {
            var leftBool = Evaluate(binary.Left);
            return leftBool.IsTruthy() ? SoloValue.FromBool(true) : SoloValue.FromBool(Evaluate(binary.Right).IsTruthy());
        }

        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);

        return binary.OperatorToken.Kind switch
        {
            SyntaxKind.PlusToken => EvaluateAdd(left, right),
            SyntaxKind.MinusToken => Numeric(left, right, (a, b) => a - b, (a, b) => a - b),
            SyntaxKind.StarToken => Numeric(left, right, (a, b) => a * b, (a, b) => a * b),
            SyntaxKind.SlashToken => Numeric(left, right, (a, b) => a / b, (a, b) => a / b),
            SyntaxKind.PercentToken => Numeric(left, right, (a, b) => a % b, (a, b) => a % b),
            SyntaxKind.EqualsEqualsToken => SoloValue.FromBool(ValuesEqual(left, right)),
            SyntaxKind.BangEqualsToken => SoloValue.FromBool(!ValuesEqual(left, right)),
            SyntaxKind.LessToken => Compare(left, right, x => x < 0),
            SyntaxKind.LessOrEqualToken => Compare(left, right, x => x <= 0),
            SyntaxKind.GreaterToken => Compare(left, right, x => x > 0),
            SyntaxKind.GreaterOrEqualToken => Compare(left, right, x => x >= 0),
            _ => throw new RuntimeException($"Unsupported binary operator '{binary.OperatorToken.Text}'."),
        };
    }

    private static SoloValue EvaluateAdd(SoloValue left, SoloValue right)
    {
        if (left.Kind == SoloValueKind.String || right.Kind == SoloValueKind.String)
            return SoloValue.FromString(left.ToString() + right.ToString());

        return Numeric(left, right, (a, b) => a + b, (a, b) => a + b);
    }

    private static SoloValue Numeric(
        SoloValue left,
        SoloValue right,
        Func<int, int, int> ints,
        Func<double, double, double> doubles)
    {
        if (left.Kind == SoloValueKind.Int && right.Kind == SoloValueKind.Int)
            return SoloValue.FromInt(ints(left.AsInt(), right.AsInt()));

        if (left.Kind is SoloValueKind.Int or SoloValueKind.Double &&
            right.Kind is SoloValueKind.Int or SoloValueKind.Double)
            return SoloValue.FromDouble(doubles(left.AsDouble(), right.AsDouble()));

        throw new RuntimeException("Arithmetic operands must be numbers.");
    }

    private static SoloValue Compare(SoloValue left, SoloValue right, Func<int, bool> predicate)
    {
        if (left.Kind is SoloValueKind.Int or SoloValueKind.Double &&
            right.Kind is SoloValueKind.Int or SoloValueKind.Double)
        {
            return SoloValue.FromBool(predicate(left.AsDouble().CompareTo(right.AsDouble())));
        }

        if (left.Kind == SoloValueKind.String && right.Kind == SoloValueKind.String)
            return SoloValue.FromBool(predicate.Invoke(string.CompareOrdinal(left.AsString(), right.AsString())));

        throw new RuntimeException("Comparison operands are incompatible.");
    }

    private static bool ValuesEqual(SoloValue left, SoloValue right)
    {
        if (left.Kind is SoloValueKind.Int or SoloValueKind.Double &&
            right.Kind is SoloValueKind.Int or SoloValueKind.Double)
            return left.AsDouble().Equals(right.AsDouble());

        return left.Equals(right);
    }

    private SoloValue EvaluateCall(CallExpressionSyntax call)
    {
        // Special-case Console.WriteLine(...)
        if (call.Callee is MemberAccessExpressionSyntax { Expression: NameExpressionSyntax { Identifier.Text: "Console" }, MemberName.Text: "WriteLine" })
        {
            var args = call.ArgumentList.Arguments.Select(Evaluate).ToArray();
            _output.WriteLine(string.Join(" ", args.Select(a => a.ToString())));
            return SoloValue.Void;
        }

        if (call.Callee is MemberAccessExpressionSyntax memberCall)
        {
            var target = Evaluate(memberCall.Expression);
            var args = call.ArgumentList.Arguments.Select(Evaluate).ToArray();

            if (target.Kind != SoloValueKind.Object)
                throw new RuntimeException("Only objects have methods.");

            var instance = target.AsObject();
            if (!instance.Definition.Methods.TryGetValue(memberCall.MemberName.Text, out var method))
                throw new RuntimeException($"Undefined method '{memberCall.MemberName.Text}' on '{instance.Definition.Name}'.");

            return CallMethod(method, instance, args);
        }

        var callee = Evaluate(call.Callee);
        var arguments = call.ArgumentList.Arguments.Select(Evaluate).ToArray();

        return callee.Kind switch
        {
            SoloValueKind.Function => CallFunction((SoloFunction)callee.Raw!, arguments),
            SoloValueKind.NativeFunction => ((NativeFunction)callee.Raw!).Implementation(arguments),
            _ => throw new RuntimeException("Can only call functions."),
        };
    }

    private SoloValue EvaluateMemberAccess(MemberAccessExpressionSyntax member)
    {
        var target = Evaluate(member.Expression);

        if (target.Kind == SoloValueKind.Array)
        {
            if (member.MemberName.Text == "Length")
                return SoloValue.FromInt(target.AsArray().Length);

            throw new RuntimeException(
                $"Arrays only have .Length (not .{member.MemberName.Text}).",
                member.MemberName.Span,
                tip: "Try nums.Length to get how many items are in the array.");
        }

        if (target.Kind == SoloValueKind.String && member.MemberName.Text == "Length")
            return SoloValue.FromInt(target.AsString().Length);

        if (target.Kind != SoloValueKind.Object)
            throw new RuntimeException("Only objects have fields.", member.Span);

        var instance = target.AsObject();
        if (instance.Fields.TryGetValue(member.MemberName.Text, out var field))
            return field;

        if (instance.Definition.Methods.TryGetValue(member.MemberName.Text, out var method))
            return SoloValue.FromFunction(method);

        throw new RuntimeException($"Undefined member '{member.MemberName.Text}'.", member.MemberName.Span);
    }

    private void Assign(ExpressionSyntax target, SoloValue value)
    {
        switch (target)
        {
            case NameExpressionSyntax name:
                _environment.Assign(name.Identifier.Text, value);
                break;
            case MemberAccessExpressionSyntax member:
            {
                var instanceValue = Evaluate(member.Expression);
                if (instanceValue.Kind != SoloValueKind.Object)
                    throw new RuntimeException("Only objects have fields.", member.Span);

                var instance = instanceValue.AsObject();
                if (!instance.Fields.ContainsKey(member.MemberName.Text))
                    throw new RuntimeException($"Undefined field '{member.MemberName.Text}'.", member.MemberName.Span);

                instance.Fields[member.MemberName.Text] = value;
                break;
            }
            case ElementAccessExpressionSyntax elementAccess:
            {
                var arrayValue = Evaluate(elementAccess.Expression);
                var indexValue = Evaluate(elementAccess.Index);
                if (arrayValue.Kind != SoloValueKind.Array)
                    throw new RuntimeException("Only arrays can be indexed with [ ].", elementAccess.Span);
                if (indexValue.Kind != SoloValueKind.Int)
                    throw new RuntimeException("Array indexes must be int values.", elementAccess.Index.Span);
                arrayValue.AsArray().Set(indexValue.AsInt(), value);
                break;
            }
            default:
                throw new RuntimeException("Invalid assignment target.", target.Span);
        }
    }

    private SoloValue CallFunction(SoloFunction function, IReadOnlyList<SoloValue> arguments)
    {
        if (arguments.Count != function.Parameters.Count)
            throw new RuntimeException(
                $"Function '{function.Name}' expects {function.Parameters.Count} argument(s) but got {arguments.Count}.");

        var environment = new SoloEnvironment(function.Closure ?? _globals);
        for (var i = 0; i < function.Parameters.Count; i++)
            environment.Define(function.Parameters[i], arguments[i]);

        try
        {
            ExecuteBlock(function.Body, environment);
            return SoloValue.Null;
        }
        catch (ReturnException ret)
        {
            return ret.Value;
        }
    }

    private SoloValue CallMethod(SoloFunction method, SoloObject instance, IReadOnlyList<SoloValue> arguments)
    {
        if (arguments.Count != method.Parameters.Count)
            throw new RuntimeException(
                $"Method '{method.Name}' expects {method.Parameters.Count} argument(s) but got {arguments.Count}.");

        var environment = new SoloEnvironment(method.Closure ?? _globals);
        environment.Define("this", SoloValue.FromObject(instance));
        for (var i = 0; i < method.Parameters.Count; i++)
            environment.Define(method.Parameters[i], arguments[i]);

        // Bind fields into a writable view via special this handling in assignment — for MVP,
        // expose field get/set through this.field via member access (already works).
        try
        {
            ExecuteBlock(method.Body, environment);
            return SoloValue.Null;
        }
        catch (ReturnException ret)
        {
            return ret.Value;
        }
    }
}
