namespace SoloC.Compiler.Syntax;

public enum SyntaxKind
{
    // Special
    BadToken,
    EndOfFileToken,
    IdentifierToken,
    NumberToken,
    StringToken,

    // Trivia
    WhitespaceTrivia,
    LineCommentTrivia,
    BlockCommentTrivia,

    // Punctuation
    PlusToken,
    MinusToken,
    StarToken,
    SlashToken,
    PercentToken,
    BangToken,
    EqualsToken,
    BangEqualsToken,
    EqualsEqualsToken,
    LessToken,
    LessOrEqualToken,
    GreaterToken,
    GreaterOrEqualToken,
    AmpersandAmpersandToken,
    PipePipeToken,
    PlusPlusToken,
    MinusMinusToken,
    OpenParenToken,
    CloseParenToken,
    OpenBraceToken,
    CloseBraceToken,
    OpenBracketToken,
    CloseBracketToken,
    CommaToken,
    DotToken,
    SemicolonToken,
    ColonToken,

    // Keywords
    TrueKeyword,
    FalseKeyword,
    NullKeyword,
    VarKeyword,
    IfKeyword,
    ElseKeyword,
    WhileKeyword,
    ForKeyword,
    ReturnKeyword,
    ClassKeyword,
    StaticKeyword,
    VoidKeyword,
    IntKeyword,
    DoubleKeyword,
    BoolKeyword,
    StringKeyword,
    NewKeyword,
    FnKeyword,
    LetKeyword,
    PrintKeyword,

    // Nodes
    CompilationUnit,
    Parameter,
    ParameterList,
    ArgumentList,
    TypeClause,

    // Expressions
    LiteralExpression,
    NameExpression,
    UnaryExpression,
    BinaryExpression,
    ParenthesizedExpression,
    AssignmentExpression,
    CallExpression,
    MemberAccessExpression,
    ObjectCreationExpression,

    // Statements
    BlockStatement,
    ExpressionStatement,
    VariableDeclarationStatement,
    IfStatement,
    WhileStatement,
    ForStatement,
    ReturnStatement,
    EmptyStatement,

    // Members / declarations
    FunctionDeclaration,
    ClassDeclaration,
    FieldDeclaration,
    MethodDeclaration,
}
