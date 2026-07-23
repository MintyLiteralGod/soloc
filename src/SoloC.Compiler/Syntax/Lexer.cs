using System.Globalization;
using System.Text;
using SoloC.Compiler.Diagnostics;

namespace SoloC.Compiler.Syntax;

public sealed class Lexer
{
    private readonly string _text;
    private readonly DiagnosticBag _diagnostics;
    private int _position;

    private static readonly Dictionary<string, SyntaxKind> Keywords = new(StringComparer.Ordinal)
    {
        ["true"] = SyntaxKind.TrueKeyword,
        ["false"] = SyntaxKind.FalseKeyword,
        ["null"] = SyntaxKind.NullKeyword,
        ["var"] = SyntaxKind.VarKeyword,
        ["let"] = SyntaxKind.LetKeyword,
        ["if"] = SyntaxKind.IfKeyword,
        ["else"] = SyntaxKind.ElseKeyword,
        ["while"] = SyntaxKind.WhileKeyword,
        ["for"] = SyntaxKind.ForKeyword,
        ["return"] = SyntaxKind.ReturnKeyword,
        ["class"] = SyntaxKind.ClassKeyword,
        ["static"] = SyntaxKind.StaticKeyword,
        ["void"] = SyntaxKind.VoidKeyword,
        ["int"] = SyntaxKind.IntKeyword,
        ["double"] = SyntaxKind.DoubleKeyword,
        ["bool"] = SyntaxKind.BoolKeyword,
        ["string"] = SyntaxKind.StringKeyword,
        ["new"] = SyntaxKind.NewKeyword,
        ["fn"] = SyntaxKind.FnKeyword,
        ["print"] = SyntaxKind.PrintKeyword,
    };

    public Lexer(string text, DiagnosticBag diagnostics)
    {
        _text = text;
        _diagnostics = diagnostics;
    }

    private char Current => Peek(0);
    private char LookAhead => Peek(1);

    private char Peek(int offset)
    {
        var index = _position + offset;
        return index >= _text.Length ? '\0' : _text[index];
    }

    public SyntaxToken Lex()
    {
        if (_position >= _text.Length)
            return new SyntaxToken(SyntaxKind.EndOfFileToken, string.Empty, null, new TextSpan(_position, 0));

        var start = _position;

        if (char.IsWhiteSpace(Current))
        {
            while (char.IsWhiteSpace(Current))
                _position++;
            return Lex();
        }

        if (Current == '/' && LookAhead == '/')
        {
            _position += 2;
            while (Current is not '\0' and not '\n' and not '\r')
                _position++;
            return Lex();
        }

        if (Current == '/' && LookAhead == '*')
        {
            _position += 2;
            while (Current != '\0')
            {
                if (Current == '*' && LookAhead == '/')
                {
                    _position += 2;
                    break;
                }

                _position++;
            }

            return Lex();
        }

        if (char.IsDigit(Current))
            return ReadNumber(start);

        if (Current is '"' or '\'')
            return ReadString(start);

        if (char.IsLetter(Current) || Current == '_')
            return ReadIdentifierOrKeyword(start);

        return ReadOperatorOrPunctuation(start);
    }

    private SyntaxToken ReadNumber(int start)
    {
        var isDouble = false;
        while (char.IsDigit(Current))
            _position++;

        if (Current == '.' && char.IsDigit(LookAhead))
        {
            isDouble = true;
            _position++;
            while (char.IsDigit(Current))
                _position++;
        }

        var text = _text[start.._position];
        object value;
        if (isDouble)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                _diagnostics.Error($"Invalid double literal '{text}'.", new TextSpan(start, text.Length));
                d = 0;
            }

            value = d;
        }
        else
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            {
                _diagnostics.Error($"Invalid integer literal '{text}'.", new TextSpan(start, text.Length));
                i = 0;
            }

            value = i;
        }

        return new SyntaxToken(SyntaxKind.NumberToken, text, value, new TextSpan(start, text.Length));
    }

    private SyntaxToken ReadString(int start)
    {
        var quote = Current;
        _position++; // opening quote
        var builder = new StringBuilder();

        while (Current != '\0' && Current != quote)
        {
            if (Current == '\\')
            {
                _position++;
                builder.Append(Current switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    '\'' => '\'',
                    '0' => '\0',
                    _ => Current,
                });
                _position++;
                continue;
            }

            builder.Append(Current);
            _position++;
        }

        if (Current != quote)
        {
            _diagnostics.Error("Unterminated string literal.", new TextSpan(start, _position - start));
            var bad = _text[start.._position];
            return new SyntaxToken(SyntaxKind.StringToken, bad, builder.ToString(), new TextSpan(start, bad.Length));
        }

        _position++; // closing quote
        var text = _text[start.._position];
        return new SyntaxToken(SyntaxKind.StringToken, text, builder.ToString(), new TextSpan(start, text.Length));
    }

    private SyntaxToken ReadIdentifierOrKeyword(int start)
    {
        while (char.IsLetterOrDigit(Current) || Current == '_')
            _position++;

        var text = _text[start.._position];
        var kind = Keywords.TryGetValue(text, out var keyword) ? keyword : SyntaxKind.IdentifierToken;
        object? value = kind switch
        {
            SyntaxKind.TrueKeyword => true,
            SyntaxKind.FalseKeyword => false,
            SyntaxKind.NullKeyword => null,
            _ => null,
        };

        return new SyntaxToken(kind, text, value, new TextSpan(start, text.Length));
    }

    private SyntaxToken ReadOperatorOrPunctuation(int start)
    {
        SyntaxKind kind;
        switch (Current)
        {
            case '+':
                if (LookAhead == '+')
                {
                    _position += 2;
                    kind = SyntaxKind.PlusPlusToken;
                }
                else
                {
                    _position++;
                    kind = SyntaxKind.PlusToken;
                }

                break;
            case '-':
                if (LookAhead == '-')
                {
                    _position += 2;
                    kind = SyntaxKind.MinusMinusToken;
                }
                else
                {
                    _position++;
                    kind = SyntaxKind.MinusToken;
                }

                break;
            case '*':
                _position++;
                kind = SyntaxKind.StarToken;
                break;
            case '/':
                _position++;
                kind = SyntaxKind.SlashToken;
                break;
            case '%':
                _position++;
                kind = SyntaxKind.PercentToken;
                break;
            case '!':
                if (LookAhead == '=')
                {
                    _position += 2;
                    kind = SyntaxKind.BangEqualsToken;
                }
                else
                {
                    _position++;
                    kind = SyntaxKind.BangToken;
                }

                break;
            case '=':
                if (LookAhead == '=')
                {
                    _position += 2;
                    kind = SyntaxKind.EqualsEqualsToken;
                }
                else
                {
                    _position++;
                    kind = SyntaxKind.EqualsToken;
                }

                break;
            case '<':
                if (LookAhead == '=')
                {
                    _position += 2;
                    kind = SyntaxKind.LessOrEqualToken;
                }
                else
                {
                    _position++;
                    kind = SyntaxKind.LessToken;
                }

                break;
            case '>':
                if (LookAhead == '=')
                {
                    _position += 2;
                    kind = SyntaxKind.GreaterOrEqualToken;
                }
                else
                {
                    _position++;
                    kind = SyntaxKind.GreaterToken;
                }

                break;
            case '&':
                if (LookAhead == '&')
                {
                    _position += 2;
                    kind = SyntaxKind.AmpersandAmpersandToken;
                    break;
                }

                goto default;
            case '|':
                if (LookAhead == '|')
                {
                    _position += 2;
                    kind = SyntaxKind.PipePipeToken;
                    break;
                }

                goto default;
            case '(':
                _position++;
                kind = SyntaxKind.OpenParenToken;
                break;
            case ')':
                _position++;
                kind = SyntaxKind.CloseParenToken;
                break;
            case '{':
                _position++;
                kind = SyntaxKind.OpenBraceToken;
                break;
            case '}':
                _position++;
                kind = SyntaxKind.CloseBraceToken;
                break;
            case '[':
                _position++;
                kind = SyntaxKind.OpenBracketToken;
                break;
            case ']':
                _position++;
                kind = SyntaxKind.CloseBracketToken;
                break;
            case ',':
                _position++;
                kind = SyntaxKind.CommaToken;
                break;
            case '.':
                _position++;
                kind = SyntaxKind.DotToken;
                break;
            case ';':
                _position++;
                kind = SyntaxKind.SemicolonToken;
                break;
            case ':':
                _position++;
                kind = SyntaxKind.ColonToken;
                break;
            default:
                _diagnostics.Error($"Unexpected character '{Current}'.", new TextSpan(_position, 1));
                _position++;
                return new SyntaxToken(SyntaxKind.BadToken, _text[start.._position], null, new TextSpan(start, 1));
        }

        var text = _text[start.._position];
        return new SyntaxToken(kind, text, null, new TextSpan(start, text.Length));
    }
}
