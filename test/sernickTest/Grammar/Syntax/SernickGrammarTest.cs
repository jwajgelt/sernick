namespace sernickTest.Grammar.Syntax;

using Input;
using Moq;
using Parser.Helpers;
using sernick.Diagnostics;
using sernick.Grammar.Lexicon;
using sernick.Grammar.Syntax;
using sernick.Input;
using sernick.Parser.ParseTree;
using sernick.Utility;
using Parser = sernick.Parser.Parser<sernick.Grammar.Syntax.Symbol>;

public class SernickGrammarTest
{
    private static readonly Range<ILocation> locations = new(new FakeLocation(), new FakeLocation());

    [Theory]
    [MemberData(nameof(TestData))]
    public void TestEdgeCases(IEnumerable<Symbol> leaves, IFakeParseTree expectedParseTree)
    {
        var grammar = SernickGrammar.Create();
        var parser = Parser.FromGrammar(grammar, Symbol.Of(NonTerminalSymbol.Start));
        var parseTree = parser.Process(
            leaves.Select(symbol => new ParseTreeLeaf<Symbol>(symbol, locations)),
            new Mock<IDiagnostics>().Object);

        var productions = grammar.Productions
            .GroupBy(production => production.Left)
            .ToDictionary(group => group.Key, group => group.ToList());
        Assert.Equal(expectedParseTree.Convert(productions), parseTree);
    }

    public static readonly IEnumerable<object[]> TestData = new[]
    {
        // {} {} + 2
        new object[]
        {
            // Input
            new[]
            {
                Symbol.Of(LexicalGrammarCategory.BracesAndParentheses, "{"),
                Symbol.Of(LexicalGrammarCategory.BracesAndParentheses, "}"),
                Symbol.Of(LexicalGrammarCategory.BracesAndParentheses, "{"),
                Symbol.Of(LexicalGrammarCategory.BracesAndParentheses, "}"),
                Symbol.Of(LexicalGrammarCategory.Operators, "+"),
                Symbol.Of(LexicalGrammarCategory.Literals, "2")
            },
            // Output
            new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.Program),
                new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ExpressionSeq),
                    new[]
                    {
                        new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.CodeBlock),
                            new IFakeParseTree[]
                            {
                                new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.BracesAndParentheses, "{")),
                                new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ExpressionSeq)),
                                new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.BracesAndParentheses, "}"))
                            }),
                        new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.OpenExpression), 1,
                            new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.LogicalOperand),
                                new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ComparisonOperand),
                                    new IFakeParseTree[]
                                    {
                                        new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.CodeBlock),
                                            new IFakeParseTree[]
                                            {
                                                new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.BracesAndParentheses, "{")),
                                                new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ExpressionSeq)),
                                                new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.BracesAndParentheses, "}"))
                                            }),
                                        new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ArithmeticOperator),
                                            new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.Operators, "+"))),
                                        new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ArithmeticOperand),
                                            new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.SimpleExpression),
                                                    new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.LiteralValue),
                                                            new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.Literals, "2")))))
                                    })))
                    }))
        },
        // var x = 5 + 5
        new object[]
        {
            // Input
            new[]
            {
                Symbol.Of(LexicalGrammarCategory.Keywords, "var"),
                Symbol.Of(LexicalGrammarCategory.VariableIdentifiers, "x"),
                Symbol.Of(LexicalGrammarCategory.Operators, "="),
                Symbol.Of(LexicalGrammarCategory.Literals, "5"),
                Symbol.Of(LexicalGrammarCategory.Operators, "+"),
                Symbol.Of(LexicalGrammarCategory.Literals, "5")
            },
            // Output
            new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.Program),
                new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ExpressionSeq),
                    new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.OpenExpression),
                        new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.VariableDeclaration),
                            new[]
                            {
                                new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.Modifier),
                                    new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.Keywords, "var"))),
                                new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.Assignment),
                                    new IFakeParseTree[]
                                    {
                                        new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.VariableIdentifiers, "x")),
                                        new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.Operators, "=")),
                                        new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.OpenExpression), 1,
                                            new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.LogicalOperand),
                                                new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ComparisonOperand),
                                                    new IFakeParseTree[]
                                                    {
                                                        new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ArithmeticOperand),
                                                            new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.SimpleExpression),
                                                                new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.LiteralValue),
                                                                    new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.Literals, "5"))))),
                                                        new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ArithmeticOperator),
                                                            new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.Operators, "+"))),
                                                        new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.ArithmeticOperand),
                                                            new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.SimpleExpression),
                                                                new FakeParseTreeNode(Symbol.Of(NonTerminalSymbol.LiteralValue),
                                                                    new FakeParseTreeLeaf(Symbol.Of(LexicalGrammarCategory.Literals, "5")))))
                                                    })))
                                    })
                            }))))
        }
    };
}
