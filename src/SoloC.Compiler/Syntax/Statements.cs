using SoloC.Compiler.Diagnostics;

namespace SoloC.Compiler.Syntax;

public abstract class StatementSyntax : SyntaxNode;

public sealed class BlockStatementSyntax : StatementSyntax
{
    public BlockStatementSyntax(
        SyntaxToken openBrace,
        IReadOnlyList<StatementSyntax> statements,
        SyntaxToken closeBrace)
    {
        OpenBraceToken = openBrace;
        Statements = statements;
        CloseBraceToken = closeBrace;
    }

    public override SyntaxKind Kind => SyntaxKind.BlockStatement;
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<StatementSyntax> Statements { get; }
    public SyntaxToken CloseBraceToken { get; }

    public override TextSpan Span =>
        new(OpenBraceToken.Span.Start, CloseBraceToken.Span.End - OpenBraceToken.Span.Start);
}

public sealed class ExpressionStatementSyntax : StatementSyntax
{
    public ExpressionStatementSyntax(ExpressionSyntax expression, SyntaxToken semicolon)
    {
        Expression = expression;
        SemicolonToken = semicolon;
    }

    public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;
    public ExpressionSyntax Expression { get; }
    public SyntaxToken SemicolonToken { get; }

    public override TextSpan Span =>
        new(Expression.Span.Start, SemicolonToken.Span.End - Expression.Span.Start);
}

public sealed class VariableDeclarationStatementSyntax : StatementSyntax
{
    public VariableDeclarationStatementSyntax(
        SyntaxToken keywordOrType,
        SyntaxToken identifier,
        SyntaxToken? equalsToken,
        ExpressionSyntax? initializer,
        SyntaxToken semicolon,
        bool isImmutable)
    {
        KeywordOrType = keywordOrType;
        Identifier = identifier;
        EqualsToken = equalsToken;
        Initializer = initializer;
        SemicolonToken = semicolon;
        IsImmutable = isImmutable;
    }

    public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;
    public SyntaxToken KeywordOrType { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken? EqualsToken { get; }
    public ExpressionSyntax? Initializer { get; }
    public SyntaxToken SemicolonToken { get; }
    public bool IsImmutable { get; }

    public override TextSpan Span =>
        new(KeywordOrType.Span.Start, SemicolonToken.Span.End - KeywordOrType.Span.Start);
}

public sealed class IfStatementSyntax : StatementSyntax
{
    public IfStatementSyntax(
        SyntaxToken ifKeyword,
        SyntaxToken openParen,
        ExpressionSyntax condition,
        SyntaxToken closeParen,
        StatementSyntax thenStatement,
        SyntaxToken? elseKeyword,
        StatementSyntax? elseStatement)
    {
        IfKeyword = ifKeyword;
        OpenParenToken = openParen;
        Condition = condition;
        CloseParenToken = closeParen;
        ThenStatement = thenStatement;
        ElseKeyword = elseKeyword;
        ElseStatement = elseStatement;
    }

    public override SyntaxKind Kind => SyntaxKind.IfStatement;
    public SyntaxToken IfKeyword { get; }
    public SyntaxToken OpenParenToken { get; }
    public ExpressionSyntax Condition { get; }
    public SyntaxToken CloseParenToken { get; }
    public StatementSyntax ThenStatement { get; }
    public SyntaxToken? ElseKeyword { get; }
    public StatementSyntax? ElseStatement { get; }

    public override TextSpan Span
    {
        get
        {
            var end = ElseStatement?.Span.End ?? ThenStatement.Span.End;
            return new TextSpan(IfKeyword.Span.Start, end - IfKeyword.Span.Start);
        }
    }
}

public sealed class WhileStatementSyntax : StatementSyntax
{
    public WhileStatementSyntax(
        SyntaxToken whileKeyword,
        SyntaxToken openParen,
        ExpressionSyntax condition,
        SyntaxToken closeParen,
        StatementSyntax body)
    {
        WhileKeyword = whileKeyword;
        OpenParenToken = openParen;
        Condition = condition;
        CloseParenToken = closeParen;
        Body = body;
    }

    public override SyntaxKind Kind => SyntaxKind.WhileStatement;
    public SyntaxToken WhileKeyword { get; }
    public SyntaxToken OpenParenToken { get; }
    public ExpressionSyntax Condition { get; }
    public SyntaxToken CloseParenToken { get; }
    public StatementSyntax Body { get; }

    public override TextSpan Span =>
        new(WhileKeyword.Span.Start, Body.Span.End - WhileKeyword.Span.Start);
}

public sealed class ForStatementSyntax : StatementSyntax
{
    public ForStatementSyntax(
        SyntaxToken forKeyword,
        SyntaxToken openParen,
        StatementSyntax? initializer,
        ExpressionSyntax? condition,
        ExpressionSyntax? increment,
        SyntaxToken closeParen,
        StatementSyntax body)
    {
        ForKeyword = forKeyword;
        OpenParenToken = openParen;
        Initializer = initializer;
        Condition = condition;
        Increment = increment;
        CloseParenToken = closeParen;
        Body = body;
    }

    public override SyntaxKind Kind => SyntaxKind.ForStatement;
    public SyntaxToken ForKeyword { get; }
    public SyntaxToken OpenParenToken { get; }
    public StatementSyntax? Initializer { get; }
    public ExpressionSyntax? Condition { get; }
    public ExpressionSyntax? Increment { get; }
    public SyntaxToken CloseParenToken { get; }
    public StatementSyntax Body { get; }

    public override TextSpan Span =>
        new(ForKeyword.Span.Start, Body.Span.End - ForKeyword.Span.Start);
}

public sealed class ReturnStatementSyntax : StatementSyntax
{
    public ReturnStatementSyntax(SyntaxToken returnKeyword, ExpressionSyntax? expression, SyntaxToken semicolon)
    {
        ReturnKeyword = returnKeyword;
        Expression = expression;
        SemicolonToken = semicolon;
    }

    public override SyntaxKind Kind => SyntaxKind.ReturnStatement;
    public SyntaxToken ReturnKeyword { get; }
    public ExpressionSyntax? Expression { get; }
    public SyntaxToken SemicolonToken { get; }

    public override TextSpan Span =>
        new(ReturnKeyword.Span.Start, SemicolonToken.Span.End - ReturnKeyword.Span.Start);
}

public sealed class EmptyStatementSyntax : StatementSyntax
{
    public EmptyStatementSyntax(SyntaxToken semicolon)
    {
        SemicolonToken = semicolon;
    }

    public override SyntaxKind Kind => SyntaxKind.EmptyStatement;
    public SyntaxToken SemicolonToken { get; }
    public override TextSpan Span => SemicolonToken.Span;
}

/// <summary>
/// Top-level statement wrapped as a member so scripts can mix declarations and statements.
/// </summary>
public sealed class GlobalStatementSyntax : MemberSyntax
{
    public GlobalStatementSyntax(StatementSyntax statement)
    {
        Statement = statement;
    }

    public override SyntaxKind Kind => Statement.Kind;
    public StatementSyntax Statement { get; }
    public override TextSpan Span => Statement.Span;
}
