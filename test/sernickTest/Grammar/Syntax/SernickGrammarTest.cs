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
using Utility;
using static Parser.Helpers.ParseTreeDsl;
using Parser = sernick.Parser.Parser<sernick.Grammar.Syntax.Symbol>;

public class SernickGrammarTest
{
    private static readonly Range<ILocation> locations = new(new FakeLocation(), new FakeLocation());

    [Theory]
    [MemberTupleData(nameof(TestData))]
    public void TestEdgeCases(IEnumerable<Symbol> leaves, IParseTree<Symbol> expectedParseTree)
    {
        var grammar = SernickGrammar.Create();
        var parser = Parser.FromGrammar(grammar, Symbol.Of(NonTerminalSymbol.Start));
        var parseTree = parser.Process(
            leaves.Select(symbol => new ParseTreeLeaf<Symbol>(symbol, locations)),
            new Mock<IDiagnostics>().Object);

        Assert.Equal(expectedParseTree, parseTree, new ParseTreeStructuralComparer());
    }

    public static readonly (IEnumerable<Symbol> leaves, IParseTree<Symbol> expectedParseTree)[] TestData = {
        // {} {} + 2
        (
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
            PT.Program(
                PT.ExpressionSeq(
                    PT.CodeBlock(
                        PT.BracesAndParentheses("{"),
                        PT.ExpressionSeq,
                        PT.BracesAndParentheses("}")),
                    PT.OpenExpression(
                        PT.LogicalOperand(
                            PT.ComparisonOperand(
                                PT.CodeBlock(
                                    PT.BracesAndParentheses("{"),
                                    PT.ExpressionSeq,
                                    PT.BracesAndParentheses("}")),
                                PT.ArithmeticOperator(
                                    PT.Operators("+")),
                                PT.ArithmeticOperand(
                                    PT.PointerOperand(
                                        PT.SimpleExpression(
                                            PT.LiteralValue(
                                                PT.Literals("2"))))))))))
        ),
        // var x = 5 + 5
        (
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
            PT.Program(
                PT.ExpressionSeq(
                    PT.OpenExpression(
                        PT.VariableDeclaration(
                            PT.Modifier(
                                PT.Keywords("var")),
                            PT.Assignment(
                                PT.VariableIdentifiers("x"),
                                PT.Operators("="),
                                PT.OpenExpression(
                                    PT.LogicalOperand(
                                        PT.ComparisonOperand(
                                            PT.ArithmeticOperand(
                                                PT.PointerOperand(
                                                    PT.SimpleExpression(
                                                        PT.LiteralValue(
                                                            PT.Literals("5"))))),
                                            PT.ArithmeticOperator(
                                                PT.Operators("+")),
                                            PT.ArithmeticOperand(
                                                PT.PointerOperand(
                                                    PT.SimpleExpression(
                                                        PT.LiteralValue(
                                                            PT.Literals("5")))))))))))))
        )
    };
}
