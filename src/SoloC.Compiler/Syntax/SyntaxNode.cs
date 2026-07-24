using SoloC.Compiler.Diagnostics;

namespace SoloC.Compiler.Syntax;

public abstract class SyntaxNode
{
    public abstract SyntaxKind Kind { get; }
    public abstract TextSpan Span { get; }
}

public sealed class CompilationUnitSyntax : SyntaxNode
{
    public CompilationUnitSyntax(IReadOnlyList<MemberSyntax> members, SyntaxToken endOfFileToken)
    {
        Members = members;
        EndOfFileToken = endOfFileToken;
    }

    public override SyntaxKind Kind => SyntaxKind.CompilationUnit;
    public IReadOnlyList<MemberSyntax> Members { get; }
    public SyntaxToken EndOfFileToken { get; }

    public override TextSpan Span
    {
        get
        {
            if (Members.Count == 0)
                return EndOfFileToken.Span;

            var start = Members[0].Span.Start;
            var end = Members[^1].Span.End;
            return new TextSpan(start, end - start);
        }
    }
}

public abstract class MemberSyntax : SyntaxNode;

public sealed class FunctionDeclarationSyntax : MemberSyntax
{
    public FunctionDeclarationSyntax(
        SyntaxToken fnKeyword,
        SyntaxToken identifier,
        ParameterListSyntax parameters,
        TypeClauseSyntax? returnType,
        BlockStatementSyntax body)
    {
        FnKeyword = fnKeyword;
        Identifier = identifier;
        Parameters = parameters;
        ReturnType = returnType;
        Body = body;
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionDeclaration;
    public SyntaxToken FnKeyword { get; }
    public SyntaxToken Identifier { get; }
    public ParameterListSyntax Parameters { get; }
    public TypeClauseSyntax? ReturnType { get; }
    public BlockStatementSyntax Body { get; }

    public override TextSpan Span =>
        new(FnKeyword.Span.Start, Body.Span.End - FnKeyword.Span.Start);
}

public sealed class ClassDeclarationSyntax : MemberSyntax
{
    public ClassDeclarationSyntax(
        SyntaxToken classKeyword,
        SyntaxToken identifier,
        SyntaxToken openBrace,
        IReadOnlyList<MemberSyntax> members,
        SyntaxToken closeBrace)
    {
        ClassKeyword = classKeyword;
        Identifier = identifier;
        OpenBraceToken = openBrace;
        Members = members;
        CloseBraceToken = closeBrace;
    }

    public override SyntaxKind Kind => SyntaxKind.ClassDeclaration;
    public SyntaxToken ClassKeyword { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken OpenBraceToken { get; }
    public IReadOnlyList<MemberSyntax> Members { get; }
    public SyntaxToken CloseBraceToken { get; }

    public override TextSpan Span =>
        new(ClassKeyword.Span.Start, CloseBraceToken.Span.End - ClassKeyword.Span.Start);
}

public sealed class MethodDeclarationSyntax : MemberSyntax
{
    public MethodDeclarationSyntax(
        SyntaxToken? staticKeyword,
        TypeClauseSyntax returnType,
        SyntaxToken identifier,
        ParameterListSyntax parameters,
        BlockStatementSyntax body)
    {
        StaticKeyword = staticKeyword;
        ReturnType = returnType;
        Identifier = identifier;
        Parameters = parameters;
        Body = body;
    }

    public override SyntaxKind Kind => SyntaxKind.MethodDeclaration;
    public SyntaxToken? StaticKeyword { get; }
    public TypeClauseSyntax ReturnType { get; }
    public SyntaxToken Identifier { get; }
    public ParameterListSyntax Parameters { get; }
    public BlockStatementSyntax Body { get; }
    public bool IsStatic => StaticKeyword is not null;

    public override TextSpan Span
    {
        get
        {
            var start = StaticKeyword?.Span.Start ?? ReturnType.Span.Start;
            return new TextSpan(start, Body.Span.End - start);
        }
    }
}

public sealed class FieldDeclarationSyntax : MemberSyntax
{
    public FieldDeclarationSyntax(
        TypeClauseSyntax type,
        SyntaxToken identifier,
        SyntaxToken? equalsToken,
        ExpressionSyntax? initializer,
        SyntaxToken semicolon)
    {
        Type = type;
        Identifier = identifier;
        EqualsToken = equalsToken;
        Initializer = initializer;
        SemicolonToken = semicolon;
    }

    public override SyntaxKind Kind => SyntaxKind.FieldDeclaration;
    public TypeClauseSyntax Type { get; }
    public SyntaxToken Identifier { get; }
    public SyntaxToken? EqualsToken { get; }
    public ExpressionSyntax? Initializer { get; }
    public SyntaxToken SemicolonToken { get; }

    public override TextSpan Span =>
        new(Type.Span.Start, SemicolonToken.Span.End - Type.Span.Start);
}

public sealed class ParameterListSyntax : SyntaxNode
{
    public ParameterListSyntax(
        SyntaxToken openParen,
        IReadOnlyList<ParameterSyntax> parameters,
        SyntaxToken closeParen)
    {
        OpenParenToken = openParen;
        Parameters = parameters;
        CloseParenToken = closeParen;
    }

    public override SyntaxKind Kind => SyntaxKind.ParameterList;
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<ParameterSyntax> Parameters { get; }
    public SyntaxToken CloseParenToken { get; }

    public override TextSpan Span =>
        new(OpenParenToken.Span.Start, CloseParenToken.Span.End - OpenParenToken.Span.Start);
}

public sealed class ParameterSyntax : SyntaxNode
{
    public ParameterSyntax(TypeClauseSyntax type, SyntaxToken identifier)
    {
        Type = type;
        Identifier = identifier;
    }

    public override SyntaxKind Kind => SyntaxKind.Parameter;
    public TypeClauseSyntax Type { get; }
    public SyntaxToken Identifier { get; }

    public override TextSpan Span =>
        new(Type.Span.Start, Identifier.Span.End - Type.Span.Start);
}

public sealed class TypeClauseSyntax : SyntaxNode
{
    public TypeClauseSyntax(SyntaxToken typeToken, bool isArray = false, SyntaxToken? openBracket = null, SyntaxToken? closeBracket = null)
    {
        TypeToken = typeToken;
        IsArray = isArray;
        OpenBracketToken = openBracket;
        CloseBracketToken = closeBracket;
    }

    public override SyntaxKind Kind => SyntaxKind.TypeClause;
    public SyntaxToken TypeToken { get; }
    public bool IsArray { get; }
    public SyntaxToken? OpenBracketToken { get; }
    public SyntaxToken? CloseBracketToken { get; }

    public string TypeName => IsArray ? $"{TypeToken.Text}[]" : TypeToken.Text;

    public override TextSpan Span
    {
        get
        {
            if (!IsArray || CloseBracketToken is null)
                return TypeToken.Span;

            return new TextSpan(TypeToken.Span.Start, CloseBracketToken.Span.End - TypeToken.Span.Start);
        }
    }
}

public sealed class UsingDirectiveSyntax : MemberSyntax
{
    public UsingDirectiveSyntax(SyntaxToken usingKeyword, SyntaxToken name, SyntaxToken semicolon)
    {
        UsingKeyword = usingKeyword;
        Name = name;
        SemicolonToken = semicolon;
    }

    public override SyntaxKind Kind => SyntaxKind.UsingDirective;
    public SyntaxToken UsingKeyword { get; }
    public SyntaxToken Name { get; }
    public SyntaxToken SemicolonToken { get; }

    public override TextSpan Span =>
        new(UsingKeyword.Span.Start, SemicolonToken.Span.End - UsingKeyword.Span.Start);
}

public sealed class ArgumentListSyntax : SyntaxNode
{
    public ArgumentListSyntax(
        SyntaxToken openParen,
        IReadOnlyList<ExpressionSyntax> arguments,
        SyntaxToken closeParen)
    {
        OpenParenToken = openParen;
        Arguments = arguments;
        CloseParenToken = closeParen;
    }

    public override SyntaxKind Kind => SyntaxKind.ArgumentList;
    public SyntaxToken OpenParenToken { get; }
    public IReadOnlyList<ExpressionSyntax> Arguments { get; }
    public SyntaxToken CloseParenToken { get; }

    public override TextSpan Span =>
        new(OpenParenToken.Span.Start, CloseParenToken.Span.End - OpenParenToken.Span.Start);
}
