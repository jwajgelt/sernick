namespace sernickTest.Ast.Nodes;

using Parser.Helpers;
using sernick.Ast;
using sernick.Ast.Nodes;
using sernick.Grammar.Lexicon;
using sernick.Grammar.Syntax;

public class ConversionTest
{
    [Fact]
    public void EmptyExpression_Conversion()
    {
        var parseTree = Fake.Node(NonTerminalSymbol.Program,
            Fake.Node(NonTerminalSymbol.ExpressionSeq, Array.Empty<IFakeParseTree>())
        ).Convert();
        
        var unit = new UnitExpression((IFakeParseTree.Locations.End, IFakeParseTree.Locations.End));
        Assert.Equal(unit, AstNode.From(parseTree));
    }

    [Fact]
    public void BreakKeyword_Conversion()
    {
        var input = Fake.Leaf(LexicalGrammarCategory.Keywords, "break").Convert();
        var breakStatement = new BreakStatement(IFakeParseTree.Locations);
        Assert.Equal(breakStatement, AstNode.From(input));
    }

    [Fact]
    public void ContinueKeyword_Conversion()
    {
        var input = Fake.Leaf(LexicalGrammarCategory.Keywords, "continue").Convert();
        var continueStatement = new ContinueStatement(IFakeParseTree.Locations);
        Assert.Equal(continueStatement, AstNode.From(input));
    }

    [Fact]
    public void ReturnExpression_Conversion()
    {
        var input = Fake.Node(NonTerminalSymbol.ReturnExpression,
            Fake.Leaf(LexicalGrammarCategory.Keywords, "return")).Convert();
        var returnStatement = new ReturnStatement(null, IFakeParseTree.Locations);
        Assert.Equal(returnStatement, AstNode.From(input));
    }

    [Fact]
    public void Bracketed_OpenedExpression_Conversion()
    {
        // { x }
        var input = Fake.Node(NonTerminalSymbol.CodeBlock,
            Fake.Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
            Fake.Node(NonTerminalSymbol.ExpressionSeq,
                Fake.Node(NonTerminalSymbol.OpenExpression,
                    Fake.Node(NonTerminalSymbol.LogicalOperand,
                        Fake.Node(NonTerminalSymbol.ComparisonOperand,
                            Fake.Node(NonTerminalSymbol.ArithmeticOperand,
                                Fake.Node(NonTerminalSymbol.SimpleExpression,
                                    Fake.Leaf(LexicalGrammarCategory.VariableIdentifiers, "x")
                                    )))))),
            Fake.Leaf(LexicalGrammarCategory.BracesAndParentheses, "}")
            ).Convert();

        var ast = (CodeBlock)AstNode.From(input);
        var variableValue = new VariableValue(new Identifier("x", IFakeParseTree.Locations),
            IFakeParseTree.Locations);

        Assert.Equal(variableValue, ast.Inner);
    }

    [Fact]
    public void Bracketed_ClosedExpression_Conversion()
    {
        // { x; }
        var input = Fake.Node(NonTerminalSymbol.CodeBlock,
            Fake.Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
            Fake.Node(NonTerminalSymbol.ExpressionSeq,
                Fake.Node(NonTerminalSymbol.OpenExpression,
                    Fake.Node(NonTerminalSymbol.LogicalOperand,
                        Fake.Node(NonTerminalSymbol.ComparisonOperand,
                            Fake.Node(NonTerminalSymbol.ArithmeticOperand,
                                Fake.Node(NonTerminalSymbol.SimpleExpression,
                                    Fake.Leaf(LexicalGrammarCategory.VariableIdentifiers, "x")
                                ))))),
                Fake.Leaf(LexicalGrammarCategory.Semicolon, ";")
                ),
            Fake.Leaf(LexicalGrammarCategory.BracesAndParentheses, "}")
        ).Convert();
        var ast = (CodeBlock)AstNode.From(input);
        var innerExpression = (ExpressionJoin)ast.Inner;

        // Converter should add Unit type after "x" so that the CodeBlock has the right type
        var variableValue = new VariableValue(new Identifier("x", IFakeParseTree.Locations),
            IFakeParseTree.Locations);
        var unit = new UnitExpression((IFakeParseTree.Locations.End, IFakeParseTree.Locations.End));
        Assert.Equal(variableValue, innerExpression.First);
        Assert.Equal(unit, innerExpression.Second);
    }

    [Fact]
    public void ArithmeticExpression_Conversion()
    {
        // 1 + 2
        var input = Fake.Node(NonTerminalSymbol.ComparisonOperand,
            Fake.Node(NonTerminalSymbol.ArithmeticOperand,
                Fake.Node(NonTerminalSymbol.SimpleExpression,
                    Fake.Node(NonTerminalSymbol.LiteralValue,
                        Fake.Leaf(LexicalGrammarCategory.Literals, "1")))),
            Fake.Node(NonTerminalSymbol.ArithmeticOperator,
                Fake.Leaf(LexicalGrammarCategory.Operators, "+")),
            Fake.Node(NonTerminalSymbol.ArithmeticOperand,
                Fake.Node(NonTerminalSymbol.SimpleExpression,
                    Fake.Node(NonTerminalSymbol.LiteralValue,
                        Fake.Leaf(LexicalGrammarCategory.Literals, "2"))))
        ).Convert();
        var expression = new Infix(
            new IntLiteralValue(1, IFakeParseTree.Locations),
            new IntLiteralValue(2, IFakeParseTree.Locations),
            Infix.Op.Plus
            );
        Assert.Equal(expression, AstNode.From(input));
    }

    [Fact]
    public void FunctionDeclaration_Conversion()
    {
        // fun foo(x: Int = 1) { x }
        var input = Fake.Node(NonTerminalSymbol.FunctionDeclaration,
            Fake.Leaf(LexicalGrammarCategory.Keywords, "fun"),
            Fake.Leaf(LexicalGrammarCategory.VariableIdentifiers, "foo"),
            Fake.Leaf(LexicalGrammarCategory.BracesAndParentheses, "("),
            Fake.Node(NonTerminalSymbol.FunctionParameters,
                Fake.Node(NonTerminalSymbol.FunctionParameterWithDefaultValue,
                    Fake.Leaf(LexicalGrammarCategory.VariableIdentifiers, "x"),
                    Fake.Node(NonTerminalSymbol.TypeSpecification,
                        Fake.Leaf(LexicalGrammarCategory.Colon, ","),
                        Fake.Leaf(LexicalGrammarCategory.TypeIdentifiers, "Int")
                    ),
                    Fake.Leaf(LexicalGrammarCategory.Operators, "="),
                    Fake.Node(NonTerminalSymbol.LiteralValue,
                        Fake.Leaf(LexicalGrammarCategory.Literals, "1")
                    )
                )
            ),
            Fake.Leaf(LexicalGrammarCategory.BracesAndParentheses, ")"),
            Fake.Node(NonTerminalSymbol.CodeBlock,
                Fake.Leaf(LexicalGrammarCategory.BracesAndParentheses, "{"),
                Fake.Node(NonTerminalSymbol.ExpressionSeq,
                    Fake.Node(NonTerminalSymbol.SimpleExpression,
                        Fake.Leaf(LexicalGrammarCategory.VariableIdentifiers, "x")
                    )
                ),
                Fake.Leaf(LexicalGrammarCategory.BracesAndParentheses, "}"))
        ).Convert();
        var fun = (FunctionDefinition)AstNode.From(input);

        var xVar = new Identifier("x", IFakeParseTree.Locations);
        var expectedParameter = new FunctionParameterDeclaration(xVar, new IntType(), new IntLiteralValue(1, IFakeParseTree.Locations), IFakeParseTree.Locations);
        var expectedStatement = new VariableValue(xVar, IFakeParseTree.Locations);

        Assert.Equal(new Identifier("foo", IFakeParseTree.Locations), fun.Name);
        Assert.Single(fun.Parameters, expectedParameter);
        Assert.Single(fun.Body.Children, expectedStatement);
    }
}
internal static class Fake
{
    public static FakeParseTreeNode Node(NonTerminalSymbol nonTerminal, params IFakeParseTree[] children) => new(Symbol.Of(nonTerminal), children);
    public static FakeParseTreeLeaf Leaf(LexicalGrammarCategory category, string text) => new(Symbol.Of(category, text));
}
