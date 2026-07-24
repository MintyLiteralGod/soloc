using SoloC.Compiler.Diagnostics;
using SoloC.Compiler.Runtime;
using SoloC.Compiler.Syntax;

namespace SoloC.Compiler.Binding;

/// <summary>
/// Lightweight static type checker — catches common mistakes before running.
/// </summary>
public sealed class TypeChecker
{
    private readonly DiagnosticBag _diagnostics;
    private readonly Stack<Dictionary<string, SoloType>> _scopes = new();
    private readonly Dictionary<string, FunctionType> _functions = new(StringComparer.Ordinal);
    private SoloType? _currentReturnType;

    public TypeChecker(DiagnosticBag diagnostics)
    {
        _diagnostics = diagnostics;
        _scopes.Push(new Dictionary<string, SoloType>(StringComparer.Ordinal)
        {
            ["print"] = SoloType.Function,
            ["Console"] = SoloType.Class("Console"),
        });
    }

    public void Check(CompilationUnitSyntax unit)
    {
        foreach (var member in unit.Members)
        {
            if (member is UsingDirectiveSyntax usingDirective)
                ApplyUsing(usingDirective);
        }

        foreach (var member in unit.Members)
        {
            switch (member)
            {
                case FunctionDeclarationSyntax function:
                    DeclareFunction(function);
                    break;
                case MethodDeclarationSyntax method:
                    DeclareMethod(method);
                    break;
                case ClassDeclarationSyntax classDecl:
                    Declare(classDecl.Identifier.Text, SoloType.Class(classDecl.Identifier.Text), classDecl.Identifier.Span);
                    break;
            }
        }

        foreach (var member in unit.Members)
        {
            switch (member)
            {
                case FunctionDeclarationSyntax function:
                    CheckFunction(function);
                    break;
                case MethodDeclarationSyntax method:
                    CheckMethod(method);
                    break;
                case ClassDeclarationSyntax classDecl:
                    CheckClass(classDecl);
                    break;
                case GlobalStatementSyntax global:
                    CheckStatement(global.Statement);
                    break;
            }
        }
    }

    private void ApplyUsing(UsingDirectiveSyntax usingDirective)
    {
        if (!StdModules.Names.Contains(usingDirective.Name.Text))
        {
            _diagnostics.Error(
                $"I don't know a module named '{usingDirective.Name.Text}'.",
                usingDirective.Name.Span,
                tip: $"Try one of: {string.Join(", ", StdModules.Names)}.");
            return;
        }

        if (usingDirective.Name.Text == "Math")
        {
            Declare("abs", SoloType.Function, usingDirective.Span);
            Declare("min", SoloType.Function, usingDirective.Span);
            Declare("max", SoloType.Function, usingDirective.Span);
            Declare("sqrt", SoloType.Function, usingDirective.Span);
            Declare("pow", SoloType.Function, usingDirective.Span);
        }
    }

    private void DeclareFunction(FunctionDeclarationSyntax function)
    {
        var parameters = function.Parameters.Parameters
            .Select(p => SoloType.FromTypeName(p.Type.TypeName))
            .ToArray();
        var returnType = function.ReturnType is null
            ? SoloType.Any
            : SoloType.FromTypeName(function.ReturnType.TypeName);
        _functions[function.Identifier.Text] = new FunctionType(parameters, returnType);
        Declare(function.Identifier.Text, SoloType.Function, function.Identifier.Span);
    }

    private void DeclareMethod(MethodDeclarationSyntax method)
    {
        var parameters = method.Parameters.Parameters
            .Select(p => SoloType.FromTypeName(p.Type.TypeName))
            .ToArray();
        var returnType = SoloType.FromTypeName(method.ReturnType.TypeName);
        _functions[method.Identifier.Text] = new FunctionType(parameters, returnType);
        Declare(method.Identifier.Text, SoloType.Function, method.Identifier.Span);
    }

    private void CheckFunction(FunctionDeclarationSyntax function)
    {
        PushScope();
        foreach (var parameter in function.Parameters.Parameters)
            Declare(parameter.Identifier.Text, SoloType.FromTypeName(parameter.Type.TypeName), parameter.Identifier.Span);

        _currentReturnType = function.ReturnType is null
            ? SoloType.Any
            : SoloType.FromTypeName(function.ReturnType.TypeName);
        CheckStatement(function.Body);
        _currentReturnType = null;
        PopScope();
    }

    private void CheckMethod(MethodDeclarationSyntax method)
    {
        PushScope();
        Declare("this", SoloType.Any, method.Identifier.Span);
        foreach (var parameter in method.Parameters.Parameters)
            Declare(parameter.Identifier.Text, SoloType.FromTypeName(parameter.Type.TypeName), parameter.Identifier.Span);

        _currentReturnType = SoloType.FromTypeName(method.ReturnType.TypeName);
        CheckStatement(method.Body);
        _currentReturnType = null;
        PopScope();
    }

    private void CheckClass(ClassDeclarationSyntax classDecl)
    {
        foreach (var member in classDecl.Members)
        {
            if (member is MethodDeclarationSyntax method)
                CheckMethod(method);
        }
    }

    private void CheckStatement(StatementSyntax statement)
    {
        switch (statement)
        {
            case BlockStatementSyntax block:
                PushScope();
                foreach (var s in block.Statements)
                    CheckStatement(s);
                PopScope();
                break;
            case VariableDeclarationStatementSyntax variable:
            {
                var declared = SoloType.FromTypeName(variable.KeywordOrType.Text);
                var initType = variable.Initializer is null ? SoloType.Null : CheckExpression(variable.Initializer);
                if (declared.Kind == SoloTypeKind.Any)
                    declared = initType == SoloType.Null ? SoloType.Any : initType;

                if (variable.Initializer is not null && !initType.IsAssignableTo(declared))
                {
                    _diagnostics.Error(
                        $"Type mismatch: '{variable.Identifier.Text}' is {declared}, but you assigned {initType}.",
                        variable.Initializer.Span,
                        tip: $"Make the value a {declared}, or change the variable's type.");
                }

                Declare(variable.Identifier.Text, declared, variable.Identifier.Span);
                break;
            }
            case ExpressionStatementSyntax expr:
                CheckExpression(expr.Expression);
                break;
            case IfStatementSyntax ifStmt:
                ExpectBoolLike(ifStmt.Condition, "if");
                CheckStatement(ifStmt.ThenStatement);
                if (ifStmt.ElseStatement is not null)
                    CheckStatement(ifStmt.ElseStatement);
                break;
            case WhileStatementSyntax whileStmt:
                ExpectBoolLike(whileStmt.Condition, "while");
                CheckStatement(whileStmt.Body);
                break;
            case ForStatementSyntax forStmt:
                PushScope();
                if (forStmt.Initializer is not null)
                    CheckStatement(forStmt.Initializer);
                if (forStmt.Condition is not null)
                    ExpectBoolLike(forStmt.Condition, "for");
                if (forStmt.Increment is not null)
                    CheckExpression(forStmt.Increment);
                CheckStatement(forStmt.Body);
                PopScope();
                break;
            case ReturnStatementSyntax returnStmt:
            {
                var valueType = returnStmt.Expression is null ? SoloType.Void : CheckExpression(returnStmt.Expression);
                if (_currentReturnType is not null &&
                    _currentReturnType.Kind != SoloTypeKind.Void &&
                    _currentReturnType.Kind != SoloTypeKind.Any &&
                    returnStmt.Expression is not null &&
                    !valueType.IsAssignableTo(_currentReturnType))
                {
                    _diagnostics.Error(
                        $"This function should return {_currentReturnType}, but you're returning {valueType}.",
                        returnStmt.Expression.Span,
                        tip: $"Return a {_currentReturnType} value instead.");
                }

                break;
            }
        }
    }

    private SoloType CheckExpression(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                return literal.Value switch
                {
                    int => SoloType.Int,
                    double => SoloType.Double,
                    bool => SoloType.Bool,
                    string => SoloType.String,
                    null => SoloType.Null,
                    _ => SoloType.Any,
                };
            case NameExpressionSyntax name:
                return Lookup(name.Identifier.Text, name.Identifier.Span);
            case ParenthesizedExpressionSyntax paren:
                return CheckExpression(paren.Expression);
            case UnaryExpressionSyntax unary:
            {
                var operand = CheckExpression(unary.Operand);
                return unary.OperatorToken.Kind switch
                {
                    SyntaxKind.BangToken => SoloType.Bool,
                    SyntaxKind.PlusToken or SyntaxKind.MinusToken when operand.IsNumber => operand,
                    SyntaxKind.PlusToken or SyntaxKind.MinusToken => Error(
                        "Unary + and - only work on numbers.", unary.Span, "Try a number like 3 or 3.14."),
                    _ => SoloType.Error,
                };
            }
            case BinaryExpressionSyntax binary:
                return CheckBinary(binary);
            case AssignmentExpressionSyntax assignment:
            {
                var targetType = CheckExpression(assignment.Target);
                var valueType = CheckExpression(assignment.Expression);
                if (!valueType.IsAssignableTo(targetType))
                {
                    _diagnostics.Error(
                        $"Can't assign {valueType} to {targetType}.",
                        assignment.Expression.Span,
                        tip: "Both sides need matching types.");
                }

                return targetType;
            }
            case CallExpressionSyntax call:
            {
                if (call.Callee is NameExpressionSyntax { Identifier.Text: var fnName } &&
                    _functions.TryGetValue(fnName, out var fnType))
                {
                    if (call.ArgumentList.Arguments.Count != fnType.Parameters.Length)
                    {
                        _diagnostics.Error(
                            $"'{fnName}' expects {fnType.Parameters.Length} argument(s), but you passed {call.ArgumentList.Arguments.Count}.",
                            call.ArgumentList.Span,
                            tip: "Count the values inside the parentheses.");
                    }
                    else
                    {
                        for (var i = 0; i < fnType.Parameters.Length; i++)
                        {
                            var argType = CheckExpression(call.ArgumentList.Arguments[i]);
                            if (!argType.IsAssignableTo(fnType.Parameters[i]))
                            {
                                _diagnostics.Error(
                                    $"Argument {i + 1} of '{fnName}' should be {fnType.Parameters[i]}, but got {argType}.",
                                    call.ArgumentList.Arguments[i].Span);
                            }
                        }
                    }

                    return fnType.ReturnType;
                }

                CheckExpression(call.Callee);
                foreach (var arg in call.ArgumentList.Arguments)
                    CheckExpression(arg);
                return SoloType.Any;
            }
            case MemberAccessExpressionSyntax member:
            {
                var target = CheckExpression(member.Expression);
                if (member.MemberName.Text == "Length" &&
                    target.Kind is SoloTypeKind.Array or SoloTypeKind.String)
                    return SoloType.Int;
                return SoloType.Any;
            }
            case ObjectCreationExpressionSyntax creation:
                foreach (var arg in creation.ArgumentList.Arguments)
                    CheckExpression(arg);
                return SoloType.Class(creation.TypeName.Text);
            case ArrayLiteralExpressionSyntax array:
            {
                if (array.Elements.Count == 0)
                    return SoloType.ArrayOf(SoloType.Any);

                var elementType = CheckExpression(array.Elements[0]);
                for (var i = 1; i < array.Elements.Count; i++)
                {
                    var next = CheckExpression(array.Elements[i]);
                    if (!next.IsAssignableTo(elementType) && !elementType.IsAssignableTo(next))
                    {
                        _diagnostics.Error(
                            $"Array elements should share one type. Found {elementType} and {next}.",
                            array.Elements[i].Span,
                            tip: "Keep every item in the list the same kind of value.");
                        elementType = SoloType.Any;
                    }
                    else if (elementType.Kind == SoloTypeKind.Int && next.Kind == SoloTypeKind.Double)
                    {
                        elementType = SoloType.Double;
                    }
                }

                return SoloType.ArrayOf(elementType);
            }
            case ElementAccessExpressionSyntax element:
            {
                var target = CheckExpression(element.Expression);
                var index = CheckExpression(element.Index);
                if (target.Kind != SoloTypeKind.Array && target.Kind != SoloTypeKind.Any)
                {
                    _diagnostics.Error(
                        "Only arrays can be indexed with [ ].",
                        element.Span,
                        tip: "Make an array first: var nums = [1, 2, 3];");
                }

                if (index.Kind != SoloTypeKind.Int && index.Kind != SoloTypeKind.Any)
                {
                    _diagnostics.Error(
                        "Array indexes must be int values.",
                        element.Index.Span,
                        tip: "Use a whole number like 0, 1, or 2.");
                }

                return target.ElementType ?? SoloType.Any;
            }
            default:
                return SoloType.Any;
        }
    }

    private SoloType CheckBinary(BinaryExpressionSyntax binary)
    {
        var left = CheckExpression(binary.Left);
        var right = CheckExpression(binary.Right);

        if (left.Kind is SoloTypeKind.Any or SoloTypeKind.Error ||
            right.Kind is SoloTypeKind.Any or SoloTypeKind.Error)
        {
            return binary.OperatorToken.Kind switch
            {
                SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.StarToken
                    or SyntaxKind.SlashToken or SyntaxKind.PercentToken => SoloType.Any,
                _ => SoloType.Bool,
            };
        }

        switch (binary.OperatorToken.Kind)
        {
            case SyntaxKind.PlusToken:
                if (left.Kind == SoloTypeKind.String || right.Kind == SoloTypeKind.String)
                    return SoloType.String;
                if (left.IsNumber && right.IsNumber)
                    return left.Kind == SoloTypeKind.Double || right.Kind == SoloTypeKind.Double
                        ? SoloType.Double
                        : SoloType.Int;
                return Error("'+' needs numbers or strings.", binary.Span, "Example: 1 + 2 or \"hi\" + name");
            case SyntaxKind.MinusToken:
            case SyntaxKind.StarToken:
            case SyntaxKind.SlashToken:
            case SyntaxKind.PercentToken:
                if (left.IsNumber && right.IsNumber)
                    return left.Kind == SoloTypeKind.Double || right.Kind == SoloTypeKind.Double
                        ? SoloType.Double
                        : SoloType.Int;
                return Error("Math operators need numbers on both sides.", binary.Span);
            case SyntaxKind.EqualsEqualsToken:
            case SyntaxKind.BangEqualsToken:
            case SyntaxKind.LessToken:
            case SyntaxKind.LessOrEqualToken:
            case SyntaxKind.GreaterToken:
            case SyntaxKind.GreaterOrEqualToken:
            case SyntaxKind.AmpersandAmpersandToken:
            case SyntaxKind.PipePipeToken:
                return SoloType.Bool;
            default:
                return SoloType.Any;
        }
    }

    private void ExpectBoolLike(ExpressionSyntax expression, string construct)
    {
        var type = CheckExpression(expression);
        if (type.Kind is not (SoloTypeKind.Bool or SoloTypeKind.Any or SoloTypeKind.Error))
        {
            _diagnostics.Warning(
                $"'{construct}' conditions work best with true/false values (got {type}).",
                expression.Span,
                tip: "Comparisons like x > 0 produce true or false.");
        }
    }

    private SoloType Error(string message, TextSpan span, string? tip = null)
    {
        _diagnostics.Error(message, span, tip);
        return SoloType.Error;
    }

    private void Declare(string name, SoloType type, TextSpan span)
    {
        var scope = _scopes.Peek();
        if (!scope.TryAdd(name, type))
            scope[name] = type;
        _ = span;
    }

    private SoloType Lookup(string name, TextSpan span)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out var type))
                return type;
        }

        _diagnostics.Error(
            $"I can't find a name called '{name}'.",
            span,
            tip: "Declare it with var/let, or check the spelling.");
        return SoloType.Error;
    }

    private void PushScope() => _scopes.Push(new Dictionary<string, SoloType>(StringComparer.Ordinal));
    private void PopScope() => _scopes.Pop();

    private sealed record FunctionType(SoloType[] Parameters, SoloType ReturnType);
}
