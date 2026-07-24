using SoloC.Compiler.Diagnostics;

namespace SoloC.Compiler.Syntax;

public abstract class ExpressionSyntax : SyntaxNode;

public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    public LiteralExpressionSyntax(SyntaxToken literalToken)
    {
        LiteralToken = literalToken;
    }

    public override SyntaxKind Kind => SyntaxKind.LiteralExpression;
    public SyntaxToken LiteralToken { get; }
    public object? Value => LiteralToken.Value;
    public override TextSpan Span => LiteralToken.Span;
}

public sealed class NameExpressionSyntax : ExpressionSyntax
{
    public NameExpressionSyntax(SyntaxToken identifier)
    {
        Identifier = identifier;
    }

    public override SyntaxKind Kind => SyntaxKind.NameExpression;
    public SyntaxToken Identifier { get; }
    public override TextSpan Span => Identifier.Span;
}

public sealed class UnaryExpressionSyntax : ExpressionSyntax
{
    public UnaryExpressionSyntax(SyntaxToken operatorToken, ExpressionSyntax operand)
    {
        OperatorToken = operatorToken;
        Operand = operand;
    }

    public override SyntaxKind Kind => SyntaxKind.UnaryExpression;
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Operand { get; }

    public override TextSpan Span =>
        new(OperatorToken.Span.Start, Operand.Span.End - OperatorToken.Span.Start);
}

public sealed class BinaryExpressionSyntax : ExpressionSyntax
{
    public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
    {
        Left = left;
        OperatorToken = operatorToken;
        Right = right;
    }

    public override SyntaxKind Kind => SyntaxKind.BinaryExpression;
    public ExpressionSyntax Left { get; }
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Right { get; }

    public override TextSpan Span =>
        new(Left.Span.Start, Right.Span.End - Left.Span.Start);
}

public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
{
    public ParenthesizedExpressionSyntax(SyntaxToken openParen, ExpressionSyntax expression, SyntaxToken closeParen)
    {
        OpenParenToken = openParen;
        Expression = expression;
        CloseParenToken = closeParen;
    }

    public override SyntaxKind Kind => SyntaxKind.ParenthesizedExpression;
    public SyntaxToken OpenParenToken { get; }
    public ExpressionSyntax Expression { get; }
    public SyntaxToken CloseParenToken { get; }

    public override TextSpan Span =>
        new(OpenParenToken.Span.Start, CloseParenToken.Span.End - OpenParenToken.Span.Start);
}

public sealed class AssignmentExpressionSyntax : ExpressionSyntax
{
    public AssignmentExpressionSyntax(ExpressionSyntax target, SyntaxToken equalsToken, ExpressionSyntax expression)
    {
        Target = target;
        EqualsToken = equalsToken;
        Expression = expression;
    }

    public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;
    public ExpressionSyntax Target { get; }
    public SyntaxToken EqualsToken { get; }
    public ExpressionSyntax Expression { get; }

    public override TextSpan Span =>
        new(Target.Span.Start, Expression.Span.End - Target.Span.Start);
}

public sealed class CallExpressionSyntax : ExpressionSyntax
{
    public CallExpressionSyntax(ExpressionSyntax callee, ArgumentListSyntax argumentList)
    {
        Callee = callee;
        ArgumentList = argumentList;
    }

    public override SyntaxKind Kind => SyntaxKind.CallExpression;
    public ExpressionSyntax Callee { get; }
    public ArgumentListSyntax ArgumentList { get; }

    public override TextSpan Span =>
        new(Callee.Span.Start, ArgumentList.Span.End - Callee.Span.Start);
}

public sealed class MemberAccessExpressionSyntax : ExpressionSyntax
{
    public MemberAccessExpressionSyntax(ExpressionSyntax expression, SyntaxToken dotToken, SyntaxToken memberName)
    {
        Expression = expression;
        DotToken = dotToken;
        MemberName = memberName;
    }

    public override SyntaxKind Kind => SyntaxKind.MemberAccessExpression;
    public ExpressionSyntax Expression { get; }
    public SyntaxToken DotToken { get; }
    public SyntaxToken MemberName { get; }

    public override TextSpan Span =>
        new(Expression.Span.Start, MemberName.Span.End - Expression.Span.Start);
}

public sealed class ObjectCreationExpressionSyntax : ExpressionSyntax
{
    public ObjectCreationExpressionSyntax(
        SyntaxToken newKeyword,
        SyntaxToken typeName,
        ArgumentListSyntax argumentList)
    {
        NewKeyword = newKeyword;
        TypeName = typeName;
        ArgumentList = argumentList;
    }

    public override SyntaxKind Kind => SyntaxKind.ObjectCreationExpression;
    public SyntaxToken NewKeyword { get; }
    public SyntaxToken TypeName { get; }
    public ArgumentListSyntax ArgumentList { get; }

    public override TextSpan Span =>
        new(NewKeyword.Span.Start, ArgumentList.Span.End - NewKeyword.Span.Start);
}

public sealed class ArrayLiteralExpressionSyntax : ExpressionSyntax
{
    public ArrayLiteralExpressionSyntax(
        SyntaxToken openBracket,
        IReadOnlyList<ExpressionSyntax> elements,
        SyntaxToken closeBracket)
    {
        OpenBracketToken = openBracket;
        Elements = elements;
        CloseBracketToken = closeBracket;
    }

    public override SyntaxKind Kind => SyntaxKind.ArrayLiteralExpression;
    public SyntaxToken OpenBracketToken { get; }
    public IReadOnlyList<ExpressionSyntax> Elements { get; }
    public SyntaxToken CloseBracketToken { get; }

    public override TextSpan Span =>
        new(OpenBracketToken.Span.Start, CloseBracketToken.Span.End - OpenBracketToken.Span.Start);
}

public sealed class ElementAccessExpressionSyntax : ExpressionSyntax
{
    public ElementAccessExpressionSyntax(
        ExpressionSyntax expression,
        SyntaxToken openBracket,
        ExpressionSyntax index,
        SyntaxToken closeBracket)
    {
        Expression = expression;
        OpenBracketToken = openBracket;
        Index = index;
        CloseBracketToken = closeBracket;
    }

    public override SyntaxKind Kind => SyntaxKind.ElementAccessExpression;
    public ExpressionSyntax Expression { get; }
    public SyntaxToken OpenBracketToken { get; }
    public ExpressionSyntax Index { get; }
    public SyntaxToken CloseBracketToken { get; }

    public override TextSpan Span =>
        new(Expression.Span.Start, CloseBracketToken.Span.End - Expression.Span.Start);
}
