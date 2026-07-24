using SoloC.Compiler.Diagnostics;

namespace SoloC.Compiler.Syntax;

public sealed class Parser
{
    private readonly DiagnosticBag _diagnostics;
    private readonly List<SyntaxToken> _tokens;
    private int _position;

    public Parser(string text, DiagnosticBag? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticBag();
        var lexer = new Lexer(text, _diagnostics);
        _tokens = [];

        while (true)
        {
            var token = lexer.Lex();
            if (token.Kind != SyntaxKind.BadToken)
                _tokens.Add(token);

            if (token.Kind == SyntaxKind.EndOfFileToken)
                break;
        }
    }

    private SyntaxToken Current => Peek(0);

    private SyntaxToken Peek(int offset)
    {
        var index = _position + offset;
        return index >= _tokens.Count ? _tokens[^1] : _tokens[index];
    }

    private SyntaxToken NextToken()
    {
        var current = Current;
        _position++;
        return current;
    }

    private SyntaxToken Match(SyntaxKind kind)
    {
        if (Current.Kind == kind)
            return NextToken();

        _diagnostics.Error(
            $"Hmm — I expected {FriendlyKind(kind)} here, but found {FriendlyKind(Current.Kind)}.",
            Current.Span,
            tip: "Check for a missing character or typo on this line.");
        return new SyntaxToken(kind, string.Empty, null, Current.Span);
    }

    private static string FriendlyKind(SyntaxKind kind) => kind switch
    {
        SyntaxKind.IdentifierToken => "a name",
        SyntaxKind.NumberToken => "a number",
        SyntaxKind.StringToken => "a string",
        SyntaxKind.SemicolonToken => "';'",
        SyntaxKind.OpenBraceToken => "'{'",
        SyntaxKind.CloseBraceToken => "'}'",
        SyntaxKind.OpenParenToken => "'('",
        SyntaxKind.CloseParenToken => "')'",
        SyntaxKind.OpenBracketToken => "'['",
        SyntaxKind.CloseBracketToken => "']'",
        SyntaxKind.CommaToken => "','",
        SyntaxKind.EqualsToken => "'='",
        SyntaxKind.EndOfFileToken => "the end of the file",
        _ => kind.ToString().Replace("Keyword", "", StringComparison.Ordinal)
            .Replace("Token", "", StringComparison.Ordinal)
            .ToLowerInvariant(),
    };

    public CompilationUnitSyntax ParseCompilationUnit()
    {
        var members = new List<MemberSyntax>();

        while (Current.Kind != SyntaxKind.EndOfFileToken)
            members.Add(ParseMember());

        var eof = Match(SyntaxKind.EndOfFileToken);
        return new CompilationUnitSyntax(members, eof);
    }

    private MemberSyntax ParseMember()
    {
        if (Current.Kind == SyntaxKind.UsingKeyword)
            return ParseUsingDirective();

        if (Current.Kind == SyntaxKind.FnKeyword)
            return ParseFunctionDeclaration();

        if (Current.Kind == SyntaxKind.ClassKeyword)
            return ParseClassDeclaration();

        if (Current.Kind == SyntaxKind.StaticKeyword || IsTypeToken(Current.Kind) || Current.Kind == SyntaxKind.IdentifierToken)
        {
            // C#-style: static void Main() / int Foo()
            if (LooksLikeMethodOrField())
                return ParseMethodOrFieldAsGlobal();
        }

        return new GlobalStatementSyntax(ParseStatement());
    }

    private UsingDirectiveSyntax ParseUsingDirective()
    {
        var usingKeyword = Match(SyntaxKind.UsingKeyword);
        var name = Match(SyntaxKind.IdentifierToken);
        var semicolon = Match(SyntaxKind.SemicolonToken);
        return new UsingDirectiveSyntax(usingKeyword, name, semicolon);
    }

    private bool LooksLikeMethodOrField()
    {
        // static? type ([] )? ident (  => method
        var index = 0;
        if (Peek(index).Kind == SyntaxKind.StaticKeyword)
            index++;

        if (!IsTypeToken(Peek(index).Kind) && Peek(index).Kind != SyntaxKind.IdentifierToken)
            return false;

        index++;
        if (Peek(index).Kind == SyntaxKind.OpenBracketToken && Peek(index + 1).Kind == SyntaxKind.CloseBracketToken)
            index += 2;

        if (Peek(index).Kind != SyntaxKind.IdentifierToken)
            return false;

        index++;
        return Peek(index).Kind == SyntaxKind.OpenParenToken;
    }

    private MemberSyntax ParseMethodOrFieldAsGlobal()
    {
        SyntaxToken? staticKeyword = null;
        if (Current.Kind == SyntaxKind.StaticKeyword)
            staticKeyword = NextToken();

        var returnType = ParseTypeClause();
        var identifier = Match(SyntaxKind.IdentifierToken);
        var parameters = ParseParameterList();
        var body = ParseBlockStatement();
        return new MethodDeclarationSyntax(staticKeyword, returnType, identifier, parameters, body);
    }

    private FunctionDeclarationSyntax ParseFunctionDeclaration()
    {
        var fn = Match(SyntaxKind.FnKeyword);
        var identifier = Match(SyntaxKind.IdentifierToken);
        var parameters = ParseParameterList();

        TypeClauseSyntax? returnType = null;
        if (Current.Kind == SyntaxKind.ColonToken)
        {
            NextToken();
            returnType = ParseTypeClause();
        }

        var body = ParseBlockStatement();
        return new FunctionDeclarationSyntax(fn, identifier, parameters, returnType, body);
    }

    private ClassDeclarationSyntax ParseClassDeclaration()
    {
        var classKeyword = Match(SyntaxKind.ClassKeyword);
        var identifier = Match(SyntaxKind.IdentifierToken);
        var open = Match(SyntaxKind.OpenBraceToken);
        var members = new List<MemberSyntax>();

        while (Current.Kind is not SyntaxKind.CloseBraceToken and not SyntaxKind.EndOfFileToken)
            members.Add(ParseClassMember());

        var close = Match(SyntaxKind.CloseBraceToken);
        return new ClassDeclarationSyntax(classKeyword, identifier, open, members, close);
    }

    private MemberSyntax ParseClassMember()
    {
        SyntaxToken? staticKeyword = null;
        if (Current.Kind == SyntaxKind.StaticKeyword)
            staticKeyword = NextToken();

        var type = ParseTypeClause();
        var identifier = Match(SyntaxKind.IdentifierToken);

        if (Current.Kind == SyntaxKind.OpenParenToken)
        {
            var parameters = ParseParameterList();
            var body = ParseBlockStatement();
            return new MethodDeclarationSyntax(staticKeyword, type, identifier, parameters, body);
        }

        SyntaxToken? equals = null;
        ExpressionSyntax? initializer = null;
        if (Current.Kind == SyntaxKind.EqualsToken)
        {
            equals = NextToken();
            initializer = ParseExpression();
        }

        var semicolon = Match(SyntaxKind.SemicolonToken);
        return new FieldDeclarationSyntax(type, identifier, equals, initializer, semicolon);
    }

    private ParameterListSyntax ParseParameterList()
    {
        var open = Match(SyntaxKind.OpenParenToken);
        var parameters = new List<ParameterSyntax>();

        if (Current.Kind != SyntaxKind.CloseParenToken)
        {
            do
            {
                if (parameters.Count > 0)
                    Match(SyntaxKind.CommaToken);

                var type = ParseTypeClause();
                var name = Match(SyntaxKind.IdentifierToken);
                parameters.Add(new ParameterSyntax(type, name));
            } while (Current.Kind == SyntaxKind.CommaToken);
        }

        var close = Match(SyntaxKind.CloseParenToken);
        return new ParameterListSyntax(open, parameters, close);
    }

    private TypeClauseSyntax ParseTypeClause()
    {
        if (IsTypeToken(Current.Kind) || Current.Kind == SyntaxKind.IdentifierToken)
        {
            var typeToken = NextToken();
            if (Current.Kind == SyntaxKind.OpenBracketToken && Peek(1).Kind == SyntaxKind.CloseBracketToken)
            {
                var open = NextToken();
                var close = NextToken();
                return new TypeClauseSyntax(typeToken, isArray: true, open, close);
            }

            return new TypeClauseSyntax(typeToken);
        }

        _diagnostics.Error(
            $"I expected a type name here (like int, string, or bool).",
            Current.Span,
            tip: "Try writing a type before the variable or parameter name.");
        return new TypeClauseSyntax(new SyntaxToken(SyntaxKind.IdentifierToken, "void", null, Current.Span));
    }

    private static bool IsTypeToken(SyntaxKind kind) =>
        kind is SyntaxKind.VoidKeyword
            or SyntaxKind.IntKeyword
            or SyntaxKind.DoubleKeyword
            or SyntaxKind.BoolKeyword
            or SyntaxKind.StringKeyword
            or SyntaxKind.VarKeyword;

    private StatementSyntax ParseStatement()
    {
        return Current.Kind switch
        {
            SyntaxKind.OpenBraceToken => ParseBlockStatement(),
            SyntaxKind.VarKeyword or SyntaxKind.LetKeyword => ParseVariableDeclaration(),
            SyntaxKind.IfKeyword => ParseIfStatement(),
            SyntaxKind.WhileKeyword => ParseWhileStatement(),
            SyntaxKind.ForKeyword => ParseForStatement(),
            SyntaxKind.ReturnKeyword => ParseReturnStatement(),
            SyntaxKind.SemicolonToken => new EmptyStatementSyntax(NextToken()),
            _ when IsTypedVariableStart() => ParseTypedVariableDeclaration(),
            _ => ParseExpressionStatement(),
        };
    }

    private bool IsTypedVariableStart()
    {
        // int x = ...;  int[] nums = ...;  string name;
        if (!IsTypeToken(Current.Kind) && Current.Kind != SyntaxKind.IdentifierToken)
            return false;

        if (Current.Kind == SyntaxKind.VarKeyword || Current.Kind == SyntaxKind.VoidKeyword)
            return Current.Kind == SyntaxKind.VarKeyword;

        var index = 1;
        if (Peek(index).Kind == SyntaxKind.OpenBracketToken && Peek(index + 1).Kind == SyntaxKind.CloseBracketToken)
            index += 2;

        return Peek(index).Kind == SyntaxKind.IdentifierToken
               && Peek(index + 1).Kind is SyntaxKind.EqualsToken or SyntaxKind.SemicolonToken;
    }

    private BlockStatementSyntax ParseBlockStatement()
    {
        var open = Match(SyntaxKind.OpenBraceToken);
        var statements = new List<StatementSyntax>();

        while (Current.Kind is not SyntaxKind.CloseBraceToken and not SyntaxKind.EndOfFileToken)
            statements.Add(ParseStatement());

        var close = Match(SyntaxKind.CloseBraceToken);
        return new BlockStatementSyntax(open, statements, close);
    }

    private VariableDeclarationStatementSyntax ParseVariableDeclaration()
    {
        var keyword = NextToken();
        var identifier = Match(SyntaxKind.IdentifierToken);
        SyntaxToken? equals = null;
        ExpressionSyntax? initializer = null;

        if (Current.Kind == SyntaxKind.EqualsToken)
        {
            equals = NextToken();
            initializer = ParseExpression();
        }

        var semicolon = Match(SyntaxKind.SemicolonToken);
        return new VariableDeclarationStatementSyntax(
            keyword,
            identifier,
            equals,
            initializer,
            semicolon,
            isImmutable: keyword.Kind == SyntaxKind.LetKeyword);
    }

    private VariableDeclarationStatementSyntax ParseTypedVariableDeclaration()
    {
        // Keep the first token as KeywordOrType for span/error friendliness;
        // array brackets are consumed into the type clause separately when present.
        var typeClause = ParseTypeClause();
        var typeToken = typeClause.TypeToken;
        if (typeClause.IsArray)
        {
            // Represent array types in KeywordOrType text as "int[]" via a synthetic token.
            typeToken = new SyntaxToken(
                typeClause.TypeToken.Kind,
                typeClause.TypeName,
                null,
                typeClause.Span);
        }

        var identifier = Match(SyntaxKind.IdentifierToken);
        SyntaxToken? equals = null;
        ExpressionSyntax? initializer = null;

        if (Current.Kind == SyntaxKind.EqualsToken)
        {
            equals = NextToken();
            initializer = ParseExpression();
        }

        var semicolon = Match(SyntaxKind.SemicolonToken);
        return new VariableDeclarationStatementSyntax(
            typeToken,
            identifier,
            equals,
            initializer,
            semicolon,
            isImmutable: false);
    }

    private IfStatementSyntax ParseIfStatement()
    {
        var ifKeyword = Match(SyntaxKind.IfKeyword);
        var open = Match(SyntaxKind.OpenParenToken);
        var condition = ParseExpression();
        var close = Match(SyntaxKind.CloseParenToken);
        var thenStatement = ParseStatement();

        SyntaxToken? elseKeyword = null;
        StatementSyntax? elseStatement = null;
        if (Current.Kind == SyntaxKind.ElseKeyword)
        {
            elseKeyword = NextToken();
            elseStatement = ParseStatement();
        }

        return new IfStatementSyntax(ifKeyword, open, condition, close, thenStatement, elseKeyword, elseStatement);
    }

    private WhileStatementSyntax ParseWhileStatement()
    {
        var whileKeyword = Match(SyntaxKind.WhileKeyword);
        var open = Match(SyntaxKind.OpenParenToken);
        var condition = ParseExpression();
        var close = Match(SyntaxKind.CloseParenToken);
        var body = ParseStatement();
        return new WhileStatementSyntax(whileKeyword, open, condition, close, body);
    }

    private ForStatementSyntax ParseForStatement()
    {
        var forKeyword = Match(SyntaxKind.ForKeyword);
        var open = Match(SyntaxKind.OpenParenToken);

        StatementSyntax? initializer = null;
        if (Current.Kind != SyntaxKind.SemicolonToken)
        {
            if (Current.Kind is SyntaxKind.VarKeyword or SyntaxKind.LetKeyword || IsTypedVariableStart())
                initializer = Current.Kind is SyntaxKind.VarKeyword or SyntaxKind.LetKeyword
                    ? ParseVariableDeclaration()
                    : ParseTypedVariableDeclaration();
            else
            {
                var expr = ParseExpression();
                var semi = Match(SyntaxKind.SemicolonToken);
                initializer = new ExpressionStatementSyntax(expr, semi);
            }
        }
        else
        {
            NextToken(); // empty initializer semicolon
        }

        ExpressionSyntax? condition = null;
        if (Current.Kind != SyntaxKind.SemicolonToken)
            condition = ParseExpression();
        Match(SyntaxKind.SemicolonToken);

        ExpressionSyntax? increment = null;
        if (Current.Kind != SyntaxKind.CloseParenToken)
            increment = ParseExpression();

        var close = Match(SyntaxKind.CloseParenToken);
        var body = ParseStatement();
        return new ForStatementSyntax(forKeyword, open, initializer, condition, increment, close, body);
    }

    private ReturnStatementSyntax ParseReturnStatement()
    {
        var returnKeyword = Match(SyntaxKind.ReturnKeyword);
        ExpressionSyntax? expression = null;
        if (Current.Kind != SyntaxKind.SemicolonToken)
            expression = ParseExpression();

        var semicolon = Match(SyntaxKind.SemicolonToken);
        return new ReturnStatementSyntax(returnKeyword, expression, semicolon);
    }

    private ExpressionStatementSyntax ParseExpressionStatement()
    {
        var expression = ParseExpression();
        var semicolon = Match(SyntaxKind.SemicolonToken);
        return new ExpressionStatementSyntax(expression, semicolon);
    }

    private ExpressionSyntax ParseExpression() => ParseAssignmentExpression();

    private ExpressionSyntax ParseAssignmentExpression()
    {
        var expression = ParseBinaryExpression();

        if (Current.Kind == SyntaxKind.EqualsToken)
        {
            if (expression is not (NameExpressionSyntax or MemberAccessExpressionSyntax or ElementAccessExpressionSyntax))
            {
                _diagnostics.Error(
                    "That can't be assigned to.",
                    expression.Span,
                    tip: "Assign to a variable name, a field like this.value, or an array slot like nums[0].");
                return expression;
            }

            var equals = NextToken();
            var value = ParseAssignmentExpression();
            return new AssignmentExpressionSyntax(expression, equals, value);
        }

        return expression;
    }

    private ExpressionSyntax ParseBinaryExpression(int parentPrecedence = 0)
    {
        ExpressionSyntax left;
        var unaryPrecedence = GetUnaryOperatorPrecedence(Current.Kind);
        if (unaryPrecedence != 0 && unaryPrecedence >= parentPrecedence)
        {
            var operatorToken = NextToken();
            var operand = ParseBinaryExpression(unaryPrecedence);
            left = new UnaryExpressionSyntax(operatorToken, operand);
        }
        else
        {
            left = ParsePrimaryExpression();
        }

        while (true)
        {
            var precedence = GetBinaryOperatorPrecedence(Current.Kind);
            if (precedence == 0 || precedence <= parentPrecedence)
                break;

            var operatorToken = NextToken();
            var right = ParseBinaryExpression(precedence);
            left = new BinaryExpressionSyntax(left, operatorToken, right);
        }

        return left;
    }

    private ExpressionSyntax ParsePrimaryExpression()
    {
        switch (Current.Kind)
        {
            case SyntaxKind.OpenParenToken:
            {
                var open = NextToken();
                var expression = ParseExpression();
                var close = Match(SyntaxKind.CloseParenToken);
                return ParsePostfix(new ParenthesizedExpressionSyntax(open, expression, close));
            }
            case SyntaxKind.TrueKeyword:
            case SyntaxKind.FalseKeyword:
            case SyntaxKind.NullKeyword:
            case SyntaxKind.NumberToken:
            case SyntaxKind.StringToken:
                return ParsePostfix(new LiteralExpressionSyntax(NextToken()));
            case SyntaxKind.PrintKeyword:
            {
                // print is both keyword and callable identifier
                var token = NextToken();
                var name = new NameExpressionSyntax(
                    new SyntaxToken(SyntaxKind.IdentifierToken, token.Text, null, token.Span));
                return ParsePostfix(name);
            }
            case SyntaxKind.NewKeyword:
            {
                var newKeyword = NextToken();
                var typeName = Match(SyntaxKind.IdentifierToken);
                var args = ParseArgumentList();
                return ParsePostfix(new ObjectCreationExpressionSyntax(newKeyword, typeName, args));
            }
            case SyntaxKind.OpenBracketToken:
                return ParsePostfix(ParseArrayLiteral());
            case SyntaxKind.IdentifierToken:
            default:
            {
                var identifier = Match(SyntaxKind.IdentifierToken);
                return ParsePostfix(new NameExpressionSyntax(identifier));
            }
        }
    }

    private ArrayLiteralExpressionSyntax ParseArrayLiteral()
    {
        var open = Match(SyntaxKind.OpenBracketToken);
        var elements = new List<ExpressionSyntax>();

        if (Current.Kind != SyntaxKind.CloseBracketToken)
        {
            do
            {
                if (elements.Count > 0)
                    Match(SyntaxKind.CommaToken);

                elements.Add(ParseExpression());
            } while (Current.Kind == SyntaxKind.CommaToken);
        }

        var close = Match(SyntaxKind.CloseBracketToken);
        return new ArrayLiteralExpressionSyntax(open, elements, close);
    }

    private ExpressionSyntax ParsePostfix(ExpressionSyntax expression)
    {
        while (true)
        {
            if (Current.Kind == SyntaxKind.OpenParenToken)
            {
                var args = ParseArgumentList();
                expression = new CallExpressionSyntax(expression, args);
                continue;
            }

            if (Current.Kind == SyntaxKind.OpenBracketToken)
            {
                var open = NextToken();
                var index = ParseExpression();
                var close = Match(SyntaxKind.CloseBracketToken);
                expression = new ElementAccessExpressionSyntax(expression, open, index, close);
                continue;
            }

            if (Current.Kind == SyntaxKind.DotToken)
            {
                var dot = NextToken();
                var member = Match(SyntaxKind.IdentifierToken);
                expression = new MemberAccessExpressionSyntax(expression, dot, member);
                continue;
            }

            break;
        }

        return expression;
    }

    private ArgumentListSyntax ParseArgumentList()
    {
        var open = Match(SyntaxKind.OpenParenToken);
        var args = new List<ExpressionSyntax>();

        if (Current.Kind != SyntaxKind.CloseParenToken)
        {
            do
            {
                if (args.Count > 0)
                    Match(SyntaxKind.CommaToken);

                args.Add(ParseExpression());
            } while (Current.Kind == SyntaxKind.CommaToken);
        }

        var close = Match(SyntaxKind.CloseParenToken);
        return new ArgumentListSyntax(open, args, close);
    }

    private static int GetUnaryOperatorPrecedence(SyntaxKind kind) =>
        kind switch
        {
            SyntaxKind.PlusToken or SyntaxKind.MinusToken or SyntaxKind.BangToken => 6,
            _ => 0,
        };

    private static int GetBinaryOperatorPrecedence(SyntaxKind kind) =>
        kind switch
        {
            SyntaxKind.StarToken or SyntaxKind.SlashToken or SyntaxKind.PercentToken => 5,
            SyntaxKind.PlusToken or SyntaxKind.MinusToken => 4,
            SyntaxKind.EqualsEqualsToken or SyntaxKind.BangEqualsToken
                or SyntaxKind.LessToken or SyntaxKind.LessOrEqualToken
                or SyntaxKind.GreaterToken or SyntaxKind.GreaterOrEqualToken => 3,
            SyntaxKind.AmpersandAmpersandToken => 2,
            SyntaxKind.PipePipeToken => 1,
            _ => 0,
        };
}
