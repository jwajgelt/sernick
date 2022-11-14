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
        Assert.Equal(new EmptyExpression(IFakeParseTree.Locations), AstNode.From(parseTree));
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
    public void ArithmeticExpression_Conversion()
    {
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
