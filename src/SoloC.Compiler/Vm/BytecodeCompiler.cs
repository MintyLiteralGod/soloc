using SoloC.Compiler.Diagnostics;
using SoloC.Compiler.Runtime;
using SoloC.Compiler.Syntax;
using SoloC.Compiler.Text;

namespace SoloC.Compiler.Vm;

/// <summary>
/// Compiles SoloC scripts (no classes / user functions) into bytecode.
/// Returns null when the program needs the tree interpreter instead.
/// </summary>
public sealed class BytecodeCompiler
{
    private readonly DiagnosticBag _diagnostics;
    private readonly SourceText _source;
    private readonly Chunk _chunk = new();
    private readonly List<Local> _locals = [];
    private int _scopeDepth;
    private bool _failed;

    public BytecodeCompiler(SourceText source, DiagnosticBag diagnostics)
    {
        _source = source;
        _diagnostics = diagnostics;
    }

    public BytecodeProgram? Compile(CompilationUnitSyntax unit)
    {
        foreach (var member in unit.Members)
        {
            switch (member)
            {
                case ClassDeclarationSyntax:
                case FunctionDeclarationSyntax:
                case MethodDeclarationSyntax:
                    return null;
                case UsingDirectiveSyntax:
                    // Host installs modules before running.
                    break;
                case GlobalStatementSyntax global:
                    CompileStatement(global.Statement);
                    break;
            }
        }

        if (_failed || _diagnostics.HasErrors)
            return null;

        var line = LineAt(_source.Length);
        _chunk.Emit(OpCode.Null, line);
        _chunk.Emit(OpCode.Return, line);
        return new BytecodeProgram(_chunk);
    }

    private void CompileStatement(StatementSyntax statement)
    {
        var line = LineAt(statement.Span.Start);
        switch (statement)
        {
            case ExpressionStatementSyntax expr:
                CompileExpression(expr.Expression);
                _chunk.Emit(OpCode.Pop, line);
                break;
            case VariableDeclarationStatementSyntax variable:
                if (variable.IsImmutable)
                {
                    // Immutable lets are enforced by the interpreter today.
                    Fail("'let' runs on the interpreter so SoloC can protect immutable values.", variable.Span);
                    break;
                }

                if (variable.Initializer is not null)
                    CompileExpression(variable.Initializer);
                else
                    _chunk.Emit(OpCode.Null, line);
                DefineVariable(variable.Identifier.Text, line);
                break;
            case BlockStatementSyntax block:
                BeginScope();
                foreach (var s in block.Statements)
                    CompileStatement(s);
                EndScope(line);
                break;
            case IfStatementSyntax ifStmt:
            {
                CompileExpression(ifStmt.Condition);
                var thenJump = _chunk.EmitJump(OpCode.JumpIfFalse, line);
                _chunk.Emit(OpCode.Pop, line);
                CompileStatement(ifStmt.ThenStatement);
                var elseJump = _chunk.EmitJump(OpCode.Jump, line);
                _chunk.PatchJump(thenJump);
                _chunk.Emit(OpCode.Pop, line);
                if (ifStmt.ElseStatement is not null)
                    CompileStatement(ifStmt.ElseStatement);
                _chunk.PatchJump(elseJump);
                break;
            }
            case WhileStatementSyntax whileStmt:
            {
                var loopStart = _chunk.Code.Count;
                CompileExpression(whileStmt.Condition);
                var exitJump = _chunk.EmitJump(OpCode.JumpIfFalse, line);
                _chunk.Emit(OpCode.Pop, line);
                CompileStatement(whileStmt.Body);
                _chunk.EmitLoop(loopStart, line);
                _chunk.PatchJump(exitJump);
                _chunk.Emit(OpCode.Pop, line);
                break;
            }
            case ForStatementSyntax forStmt:
            {
                BeginScope();
                if (forStmt.Initializer is not null)
                    CompileStatement(forStmt.Initializer);

                var loopStart = _chunk.Code.Count;
                int? exitJump = null;
                if (forStmt.Condition is not null)
                {
                    CompileExpression(forStmt.Condition);
                    exitJump = _chunk.EmitJump(OpCode.JumpIfFalse, line);
                    _chunk.Emit(OpCode.Pop, line);
                }

                CompileStatement(forStmt.Body);
                if (forStmt.Increment is not null)
                {
                    CompileExpression(forStmt.Increment);
                    _chunk.Emit(OpCode.Pop, line);
                }

                _chunk.EmitLoop(loopStart, line);
                if (exitJump is int jump)
                {
                    _chunk.PatchJump(jump);
                    _chunk.Emit(OpCode.Pop, line);
                }

                EndScope(line);
                break;
            }
            case EmptyStatementSyntax:
                break;
            default:
                Fail($"This statement isn't supported by the bytecode VM yet: {statement.Kind}.", statement.Span);
                break;
        }
    }

    private void CompileExpression(ExpressionSyntax expression)
    {
        var line = LineAt(expression.Span.Start);
        switch (expression)
        {
            case LiteralExpressionSyntax literal:
                EmitLiteral(literal.Value, line);
                break;
            case NameExpressionSyntax name:
                NamedGet(name.Identifier.Text, line);
                break;
            case ParenthesizedExpressionSyntax paren:
                CompileExpression(paren.Expression);
                break;
            case UnaryExpressionSyntax unary:
                CompileExpression(unary.Operand);
                if (unary.OperatorToken.Kind == SyntaxKind.PlusToken)
                    break;
                _chunk.Emit(unary.OperatorToken.Kind switch
                {
                    SyntaxKind.MinusToken => OpCode.Negate,
                    SyntaxKind.BangToken => OpCode.Not,
                    _ => OpCode.Not,
                }, line);
                break;
            case BinaryExpressionSyntax binary:
                if (binary.OperatorToken.Kind is SyntaxKind.AmpersandAmpersandToken or SyntaxKind.PipePipeToken)
                {
                    CompileLogical(binary);
                    break;
                }

                CompileExpression(binary.Left);
                CompileExpression(binary.Right);
                _chunk.Emit(binary.OperatorToken.Kind switch
                {
                    SyntaxKind.PlusToken => OpCode.Add,
                    SyntaxKind.MinusToken => OpCode.Subtract,
                    SyntaxKind.StarToken => OpCode.Multiply,
                    SyntaxKind.SlashToken => OpCode.Divide,
                    SyntaxKind.PercentToken => OpCode.Modulo,
                    SyntaxKind.EqualsEqualsToken => OpCode.Equal,
                    SyntaxKind.BangEqualsToken => OpCode.NotEqual,
                    SyntaxKind.GreaterToken => OpCode.Greater,
                    SyntaxKind.GreaterOrEqualToken => OpCode.GreaterEqual,
                    SyntaxKind.LessToken => OpCode.Less,
                    SyntaxKind.LessOrEqualToken => OpCode.LessEqual,
                    _ => OpCode.Equal,
                }, line);
                break;
            case AssignmentExpressionSyntax assignment:
                CompileAssignment(assignment);
                break;
            case CallExpressionSyntax call:
                CompileCall(call);
                break;
            case ArrayLiteralExpressionSyntax array:
                foreach (var element in array.Elements)
                    CompileExpression(element);
                _chunk.Emit(OpCode.MakeArray, line);
                _chunk.EmitByte((byte)array.Elements.Count, line);
                break;
            case ElementAccessExpressionSyntax element:
                CompileExpression(element.Expression);
                CompileExpression(element.Index);
                _chunk.Emit(OpCode.GetIndex, line);
                break;
            case MemberAccessExpressionSyntax { MemberName.Text: "Length" } member:
                CompileExpression(member.Expression);
                _chunk.Emit(OpCode.GetLength, line);
                break;
            case MemberAccessExpressionSyntax member when
                member.Expression is NameExpressionSyntax { Identifier.Text: "Console" } &&
                member.MemberName.Text == "WriteLine":
                // Handled only as call target.
                Fail("Use Console.WriteLine(...) as a call.", member.Span);
                break;
            default:
                Fail($"This expression isn't supported by the bytecode VM yet: {expression.Kind}.", expression.Span);
                break;
        }
    }

    private void CompileLogical(BinaryExpressionSyntax binary)
    {
        var line = LineAt(binary.Span.Start);
        CompileExpression(binary.Left);
        if (binary.OperatorToken.Kind == SyntaxKind.AmpersandAmpersandToken)
        {
            var endJump = _chunk.EmitJump(OpCode.JumpIfFalse, line);
            _chunk.Emit(OpCode.Pop, line);
            CompileExpression(binary.Right);
            _chunk.PatchJump(endJump);
        }
        else
        {
            var elseJump = _chunk.EmitJump(OpCode.JumpIfFalse, line);
            var endJump = _chunk.EmitJump(OpCode.Jump, line);
            _chunk.PatchJump(elseJump);
            _chunk.Emit(OpCode.Pop, line);
            CompileExpression(binary.Right);
            _chunk.PatchJump(endJump);
        }
    }

    private void CompileAssignment(AssignmentExpressionSyntax assignment)
    {
        var line = LineAt(assignment.Span.Start);
        switch (assignment.Target)
        {
            case NameExpressionSyntax name:
                CompileExpression(assignment.Expression);
                NamedSet(name.Identifier.Text, line);
                break;
            case ElementAccessExpressionSyntax element:
                CompileExpression(element.Expression);
                CompileExpression(element.Index);
                CompileExpression(assignment.Expression);
                _chunk.Emit(OpCode.SetIndex, line);
                break;
            default:
                Fail("That assignment target isn't supported by the bytecode VM yet.", assignment.Target.Span);
                break;
        }
    }

    private void CompileCall(CallExpressionSyntax call)
    {
        var line = LineAt(call.Span.Start);
        if (call.Callee is NameExpressionSyntax { Identifier.Text: "print" } ||
            call.Callee is MemberAccessExpressionSyntax
            {
                Expression: NameExpressionSyntax { Identifier.Text: "Console" },
                MemberName.Text: "WriteLine"
            })
        {
            if (call.ArgumentList.Arguments.Count == 0)
            {
                _chunk.EmitConstant(SoloValue.FromString(string.Empty), line);
            }
            else if (call.ArgumentList.Arguments.Count == 1)
            {
                CompileExpression(call.ArgumentList.Arguments[0]);
            }
            else
            {
                CompileExpression(call.ArgumentList.Arguments[0]);
                for (var i = 1; i < call.ArgumentList.Arguments.Count; i++)
                {
                    _chunk.EmitConstant(SoloValue.FromString(" "), line);
                    _chunk.Emit(OpCode.Add, line);
                    CompileExpression(call.ArgumentList.Arguments[i]);
                    _chunk.Emit(OpCode.Add, line);
                }
            }

            _chunk.Emit(OpCode.Print, line);
            _chunk.Emit(OpCode.Null, line);
            return;
        }

        // Built-in Math functions etc.
        if (call.Callee is NameExpressionSyntax name)
        {
            foreach (var arg in call.ArgumentList.Arguments)
                CompileExpression(arg);

            NamedGet(name.Identifier.Text, line);
            _chunk.Emit(OpCode.Call, line);
            _chunk.EmitByte((byte)call.ArgumentList.Arguments.Count, line);
            return;
        }

        Fail("Only simple function calls are supported in the bytecode VM for now.", call.Span);
    }

    private void EmitLiteral(object? value, int line)
    {
        switch (value)
        {
            case null:
                _chunk.Emit(OpCode.Null, line);
                break;
            case true:
                _chunk.Emit(OpCode.True, line);
                break;
            case false:
                _chunk.Emit(OpCode.False, line);
                break;
            default:
                _chunk.EmitConstant(SoloValue.FromClr(value), line);
                break;
        }
    }

    private void DefineVariable(string name, int line)
    {
        if (_scopeDepth > 0)
        {
            _locals.Add(new Local(name, _scopeDepth));
            return; // value already on stack becomes the local
        }

        _chunk.Emit(OpCode.DefineGlobal, line);
        EmitNameConstant(name, line);
    }

    private void NamedGet(string name, int line)
    {
        if (ResolveLocal(name) is int slot)
        {
            _chunk.Emit(OpCode.GetLocal, line);
            _chunk.EmitByte((byte)slot, line);
            return;
        }

        _chunk.Emit(OpCode.GetGlobal, line);
        EmitNameConstant(name, line);
    }

    private void NamedSet(string name, int line)
    {
        if (ResolveLocal(name) is int slot)
        {
            _chunk.Emit(OpCode.SetLocal, line);
            _chunk.EmitByte((byte)slot, line);
            return;
        }

        _chunk.Emit(OpCode.SetGlobal, line);
        EmitNameConstant(name, line);
    }

    private int? ResolveLocal(string name)
    {
        for (var i = _locals.Count - 1; i >= 0; i--)
        {
            if (_locals[i].Name == name)
                return i;
        }

        return null;
    }

    private void EmitNameConstant(string name, int line)
    {
        var index = _chunk.AddConstant(SoloValue.FromString(name));
        _chunk.EmitByte((byte)index, line);
    }

    private void BeginScope() => _scopeDepth++;

    private void EndScope(int line)
    {
        while (_locals.Count > 0 && _locals[^1].Depth == _scopeDepth)
        {
            _locals.RemoveAt(_locals.Count - 1);
            _chunk.Emit(OpCode.Pop, line);
        }

        _scopeDepth--;
    }

    private void Fail(string message, TextSpan span)
    {
        _failed = true;
        _diagnostics.Info(message, span, tip: "SoloC will automatically use the interpreter for this program.");
    }

    private int LineAt(int position) => _source.GetLocation(position).Line;

    private readonly record struct Local(string Name, int Depth);
}

public sealed class BytecodeProgram
{
    public BytecodeProgram(Chunk chunk) => Chunk = chunk;
    public Chunk Chunk { get; }
}
