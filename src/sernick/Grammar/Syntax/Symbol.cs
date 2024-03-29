namespace sernick.Grammar.Syntax;

using LexicalCategory = Lexicon.LexicalGrammarCategory;

public abstract record Symbol
{
    public static Symbol Of(LexicalCategory category, string text = "") => new Terminal(category, text);
    public static Symbol Of(NonTerminalSymbol symbol) => new NonTerminal(symbol);
}

public sealed record Terminal(LexicalCategory Category, string Text) : Symbol
{
    public bool Equals(Terminal? other) => Category switch
    {
        LexicalCategory.BracesAndParentheses or LexicalCategory.Operators or LexicalCategory.Keywords => (Category, Text) == (other?.Category, other?.Text),
        _ => (Category == other?.Category)
    };

    public override int GetHashCode() => Category switch
    {
        LexicalCategory.BracesAndParentheses or LexicalCategory.Operators or LexicalCategory.Keywords => HashCode.Combine(Category, Text),
        _ => Category.GetHashCode()
    };

    public override string ToString() => $"{Category}({Text})";
}

public sealed record NonTerminal(NonTerminalSymbol Inner) : Symbol
{
    public override string ToString() => Inner.ToString();
}

public enum NonTerminalSymbol
{
    Start,
    Program,
    ExpressionSeq,
    OpenExpression,
    CodeBlock,
    CodeGroup,
    ReturnExpression,
    AssignmentOperand,
    PointerOperand,
    LogicalOperand,
    LogicalOperator,
    ComparisonOperand,
    ComparisonOperator,
    ArithmeticOperand,
    ArithmeticOperator,
    SimpleExpression,
    LiteralValue,
    FunctionCall,
    FunctionArguments,
    IfCondition,
    IfExpression,
    LoopExpression,
    StructValue,
    StructValueFields,
    StructFieldInitializer,
    Modifier,
    Type,
    TypeSpecification,
    VariableDeclaration,
    FunctionDeclaration,
    FunctionParameters,
    FunctionParameter,
    FunctionParameterWithDefaultValue,
    StructDeclaration,
    StructDeclarationFields,
    StructFieldDeclaration
}
