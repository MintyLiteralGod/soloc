using SoloC.Compiler.Diagnostics;
using SoloC.Compiler.Syntax;

namespace SoloC.Tests;

public class LexerTests
{
    [Fact]
    public void Lexes_keywords_identifiers_and_literals()
    {
        var tokens = Lex("var x = 42; print(\"hi\");");

        Assert.Contains(tokens, t => t.Kind == SyntaxKind.VarKeyword);
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.IdentifierToken && t.Text == "x");
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.NumberToken && Equals(t.Value, 42));
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.StringToken && Equals(t.Value, "hi"));
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.PrintKeyword);
    }

    [Fact]
    public void Lexes_operators()
    {
        var tokens = Lex("a == b && c != d || e <= f");
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.EqualsEqualsToken);
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.AmpersandAmpersandToken);
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.BangEqualsToken);
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.PipePipeToken);
        Assert.Contains(tokens, t => t.Kind == SyntaxKind.LessOrEqualToken);
    }

    private static List<SyntaxToken> Lex(string text)
    {
        var diagnostics = new DiagnosticBag();
        var lexer = new Lexer(text, diagnostics);
        var tokens = new List<SyntaxToken>();
        while (true)
        {
            var token = lexer.Lex();
            tokens.Add(token);
            if (token.Kind == SyntaxKind.EndOfFileToken)
                break;
        }

        Assert.False(diagnostics.HasErrors);
        return tokens;
    }
}
