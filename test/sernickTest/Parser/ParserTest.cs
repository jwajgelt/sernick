namespace sernickTest.Parser;

using Helpers;
using Input;
using Moq;
using sernick.Diagnostics;
using sernick.Input;
using sernick.Parser;
using sernick.Utility;
using Grammar = sernick.Grammar.Syntax.Grammar<Helpers.CharCategory>;
using IParseTree = sernick.Parser.ParseTree.IParseTree<Helpers.CharCategory>;
using Parser = sernick.Parser.Parser<Helpers.CharCategory>;
using ParseTreeLeaf = sernick.Parser.ParseTree.ParseTreeLeaf<Helpers.CharCategory>;
using ParseTreeNode = sernick.Parser.ParseTree.ParseTreeNode<Helpers.CharCategory>;
using Production = sernick.Grammar.Syntax.Production<Helpers.CharCategory>;
using Regex = sernick.Common.Regex.Regex<Helpers.CharCategory>;

public class ParserTest
{
    private readonly Range<ILocation> _location = new(new FakeLocation(), new FakeLocation());

    // Checked sequence is equal to start symbol
    [Fact]
    public void EmptyGrammarInstantlyAccepting()
    {
        var grammar = new Grammar('S'.ToCategory(), Array.Empty<Production>());
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('S'.ToCategory(), _location);
        var leaves = new[] { leaf };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        Assert.Equal(leaf, result);
    }

    // Checked sequence is different from start symbol
    [Fact]
    public void EmptyGrammarInstantlyRejecting()
    {
        var grammar = new Grammar('S'.ToCategory(), Array.Empty<Production>());
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('A'.ToCategory(), _location);
        var leaves = new[] { leaf };

        Assert.Throws<ParsingException<CharCategory>>(() => parser.Process(leaves, new Mock<IDiagnostics>().Object));
    }

    /*  S -> A
     * input: A
     * result:
     *   S
     *   |(S -> A)
     *   A
     */
    [Fact]
    public void OneAtomProductionGrammarCorrectInput()
    {
        var production = new Production('S'.ToCategory(), Regex.Atom('A'.ToCategory()));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('A'.ToCategory(), _location);
        var leaves = new[] { leaf };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        // expected result
        var expectedRoot = new ParseTreeNode('S'.ToCategory(), production, new[] { leaf }, _location);

        Assert.Equal(expectedRoot, result);
    }

    /*  S -> A
     * input: B
     * result: error
     */
    [Fact]
    public void OneAtomProductionGrammarIncorrectInput()
    {
        var production = new Production('S'.ToCategory(), Regex.Atom('A'.ToCategory()));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('B'.ToCategory(), _location);
        var leaves = new[] { leaf };

        Assert.Throws<ParsingException<CharCategory>>(() => parser.Process(leaves, new Mock<IDiagnostics>().Object));
    }

    /*  S -> A*
     * input: AA
     * result:
     *   S
     *   |(S -> A*)
     *   AA
     */
    [Fact]
    public void OneStarProductionGrammarCorrectInput()
    {
        var production = new Production('S'.ToCategory(), Regex.Star(Regex.Atom('A'.ToCategory())));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('A'.ToCategory(), _location);
        var leaves = new[] { leaf, leaf };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        // expected result
        var expectedRoot = new ParseTreeNode('S'.ToCategory(), production, new[] { leaf, leaf }, _location);

        Assert.Equal(expectedRoot, result);
    }

    /*  S -> A*
     * input: B
     * result: error
     */
    [Fact]
    public void OneStarProductionGrammarIncorrectInput()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Star(Regex.Atom('A'.ToCategory())));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('B'.ToCategory(), _location);
        var leaves = new[] { leaf };

        Assert.Throws<ParsingException<CharCategory>>(() => parser.Process(leaves, new Mock<IDiagnostics>().Object));
    }

    /*  S -> AB
     * input: AB
     * result:
     *   S
     *   |(S -> AB)
     *   AB
     */
    [Fact]
    public void OneConcatProductionGrammarCorrectInput()
    {
        var production = new Production('S'.ToCategory(), Regex.Concat(
            Regex.Atom('A'.ToCategory()),
            Regex.Atom('B'.ToCategory())
        ));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf1 = new ParseTreeLeaf('A'.ToCategory(), _location);
        var leaf2 = new ParseTreeLeaf('B'.ToCategory(), _location);
        var leaves = new[] { leaf1, leaf2 };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        // expected result
        var expectedRoot = new ParseTreeNode('S'.ToCategory(), production, new[] { leaf1, leaf2 }, _location);

        Assert.Equal(expectedRoot, result);
    }

    /*  S -> AB
     * input: A
     * result: error
     */
    [Fact]
    public void OneConcatProductionGrammarIncorrectInput()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Concat(
            Regex.Atom('A'.ToCategory()),
            Regex.Atom('B'.ToCategory())
        ));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf1 = new ParseTreeLeaf('A'.ToCategory(), _location);
        var leaves = new[] { leaf1 };

        Assert.Throws<ParsingException<CharCategory>>(() => parser.Process(leaves, new Mock<IDiagnostics>().Object));
    }

    /*  S -> A+B
     * input: B
     * result:
     *   S
     *   |(S -> A+B)
     *   B
     */
    [Fact]
    public void OneUnionProductionGrammarCorrectInput()
    {
        var production = new Production('S'.ToCategory(), Regex.Union(
            Regex.Atom('A'.ToCategory()),
            Regex.Atom('B'.ToCategory())
        ));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('B'.ToCategory(), _location);
        var leaves = new[] { leaf };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        // expected result
        var expectedRoot = new ParseTreeNode('S'.ToCategory(), production, new[] { leaf }, _location);

        Assert.Equal(expectedRoot, result);
    }

    /*  S -> A+B
    * input: C
    * result: error
    */
    [Fact]
    public void OneUnionProductionGrammarIncorrectInput()
    {
        var production = new Production('S'.ToCategory(), Regex.Union(
            Regex.Atom('A'.ToCategory()),
            Regex.Atom('B'.ToCategory())
        ));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('C'.ToCategory(), _location);
        var leaves = new[] { leaf };

        Assert.Throws<ParsingException<CharCategory>>(() => parser.Process(leaves, new Mock<IDiagnostics>().Object));
    }

    /*  S -> A
     *  A -> B
     *  B -> C
     *  C -> D
     * input: D
     * result:
     *   S
     *   |(S -> A)
     *   A
     *   |(A -> B)
     *   B
     *   |(B -> C)
     *   C
     *   |(C -> D)
     *   D
     */
    [Fact]
    public void SnakeGrammarCorrectInputCase1()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Atom('A'.ToCategory()));
        var production2 = new Production('A'.ToCategory(), Regex.Atom('B'.ToCategory()));
        var production3 = new Production('B'.ToCategory(), Regex.Atom('C'.ToCategory()));
        var production4 = new Production('C'.ToCategory(), Regex.Atom('D'.ToCategory()));

        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1,
            production2,
            production3,
            production4
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('D'.ToCategory(), _location);
        var leaves = new[] { leaf };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        // expected result
        var expectedNodeC = new ParseTreeNode('C'.ToCategory(), production4, new[] { leaf }, _location);
        var expectedNodeB = new ParseTreeNode('B'.ToCategory(), production3, new[] { expectedNodeC }, _location);
        var expectedNodeA = new ParseTreeNode('A'.ToCategory(), production2, new[] { expectedNodeB }, _location);
        var expectedRoot = new ParseTreeNode('S'.ToCategory(), production1, new[] { expectedNodeA }, _location);

        Assert.Equal(expectedRoot, result);
    }

    /*  S -> A
     *  A -> B
     *  B -> C
     *  C -> D
     * input: B
     * result:
     *   S
     *   |(S -> A)
     *   A
     *   |(A -> B)
     *   B
     */
    [Fact]
    public void SnakeGrammarCorrectInputCase2()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Atom('A'.ToCategory()));
        var production2 = new Production('A'.ToCategory(), Regex.Atom('B'.ToCategory()));
        var production3 = new Production('B'.ToCategory(), Regex.Atom('C'.ToCategory()));
        var production4 = new Production('C'.ToCategory(), Regex.Atom('D'.ToCategory()));

        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1,
            production2,
            production3,
            production4
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('B'.ToCategory(), _location);
        var leaves = new[] { leaf };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        // expected result
        var expectedNodeA = new ParseTreeNode('A'.ToCategory(), production2, new[] { leaf }, _location);
        var expectedRoot = new ParseTreeNode('S'.ToCategory(), production1, new[] { expectedNodeA }, _location);

        Assert.Equal(expectedRoot, result);
    }

    /*  S -> A
     *  S -> SA
     * input: AAA
     * result:
     *   S
     *   |(S -> SA)
     *   SA
     *   |(S -> SA)
     *   SA
     *   |(S -> A)
     *   A
     */
    [Fact]
    public void LeftRecursiveGrammarCorrectInput()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Atom('A'.ToCategory()));
        var production2 = new Production('S'.ToCategory(), Regex.Concat(
            Regex.Atom('S'.ToCategory()),
            Regex.Atom('A'.ToCategory())
        ));

        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1,
            production2
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('A'.ToCategory(), _location);
        var leaves = new[] { leaf, leaf, leaf };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        // expected result
        var expectedNode2 = new ParseTreeNode('S'.ToCategory(), production1, new[] { leaf }, _location);
        var expectedNode1 = new ParseTreeNode('S'.ToCategory(), production2, new IParseTree[] { expectedNode2, leaf }, _location);
        var expectedRoot = new ParseTreeNode('S'.ToCategory(), production2, new IParseTree[] { expectedNode1, leaf }, _location);

        Assert.Equal(expectedRoot, result);
    }

    /*  S -> X
     *  S -> SX
     *  X -> Yr
     *  Y -> Pa
     *  Y -> se
     * input: Parser
     * result:
     *   S
     *   |(S -> SX)
     *   S -------------- X
     *   |(S -> X)        |(X -> Yr)
     *   X                Yr
     *   |(X -> Yr)       |(Y -> se)
     *   Yr               se
     *   |(Y -> Pa)
     *   Pa
     */
    [Fact]
    public void ComplexGrammarCorrectInputCase1()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Atom('X'.ToCategory()));
        var production2 = new Production('S'.ToCategory(), Regex.Concat(
            Regex.Atom('S'.ToCategory()),
            Regex.Atom('X'.ToCategory())
        ));
        var production3 = new Production('X'.ToCategory(), Regex.Concat(
            Regex.Atom('Y'.ToCategory()),
            Regex.Atom('r'.ToCategory())
        ));
        var production4 = new Production('Y'.ToCategory(), Regex.Concat(
            Regex.Atom('P'.ToCategory()),
            Regex.Atom('a'.ToCategory())
        ));
        var production5 = new Production('Y'.ToCategory(), Regex.Concat(
            Regex.Atom('s'.ToCategory()),
            Regex.Atom('e'.ToCategory())
        ));

        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1,
            production2,
            production3,
            production4,
            production5
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf1 = new ParseTreeLeaf('P'.ToCategory(), _location);
        var leaf2 = new ParseTreeLeaf('a'.ToCategory(), _location);
        var leaf3 = new ParseTreeLeaf('r'.ToCategory(), _location);
        var leaf4 = new ParseTreeLeaf('s'.ToCategory(), _location);
        var leaf5 = new ParseTreeLeaf('e'.ToCategory(), _location);
        var leaf6 = new ParseTreeLeaf('r'.ToCategory(), _location);

        var leaves = new[] { leaf1, leaf2, leaf3, leaf4, leaf5, leaf6 };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        // expected result
        // (Y -> Pa)
        var expectedLeftNode3 =
            new ParseTreeNode('Y'.ToCategory(), production4, new[] { leaf1, leaf2 }, _location);
        // (X -> Yr)
        var expectedLeftNode2 =
            new ParseTreeNode('X'.ToCategory(), production3, new IParseTree[] { expectedLeftNode3, leaf3 }, _location);
        // (S -> X)
        var expectedLeftNode1 =
            new ParseTreeNode('S'.ToCategory(), production1, new[] { expectedLeftNode2 }, _location);
        // (Y -> se)
        var expectedRightNode2 =
            new ParseTreeNode('Y'.ToCategory(), production5, new[] { leaf4, leaf5 }, _location);
        // (X -> Yr)
        var expectedRightNode1 =
            new ParseTreeNode('X'.ToCategory(), production3, new IParseTree[] { expectedRightNode2, leaf6 }, _location);
        // (S -> SX)
        var expectedRoot =
            new ParseTreeNode('S'.ToCategory(), production2, new[] { expectedLeftNode1, expectedRightNode1 }, _location);

        Assert.Equal(expectedRoot, result);
    }

    /* Parenthesis
     *  S -> (S)*
     * input: ()(()())()
     * result:
     *                   S
     *                   | (S -> (S)(S)(S))
     *    (S)-----------(S)----------------(S)
     *     |(S -> eps)   |(S -> (S)(S))     |(S -> eps) 
     *              (S)-----------(S)             
     *               |(S -> eps)   |(S -> eps)
     */
    [Fact]
    public void ComplexGrammarCorrectInputCase2()
    {
        var production = new Production('S'.ToCategory(), Regex.Star(
            Regex.Concat(
                Regex.Atom('('.ToCategory()),
                Regex.Atom('S'.ToCategory()),
                Regex.Atom(')'.ToCategory())
            )));
        var grammar = new Grammar('S'.ToCategory(), new[] { production });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        // ()(()())()
        var leafLeft = new ParseTreeLeaf('('.ToCategory(), _location);
        var leafRight = new ParseTreeLeaf(')'.ToCategory(), _location);

        var leaves = new[]
        {
            leafLeft,
            leafRight,
            leafLeft,
            leafLeft,
            leafRight,
            leafLeft,
            leafRight,
            leafRight,
            leafLeft,
            leafRight
        };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        // expected result
        // (S -> eps)
        var emptySProductionNode =
            new ParseTreeNode('S'.ToCategory(), production, Array.Empty<IParseTree>(), _location);
        // (S -> (S)(S)) 
        var expectedMiddleNode =
            new ParseTreeNode('S'.ToCategory(), production, new IParseTree[]
            {
                leafLeft, emptySProductionNode, leafRight,
                leafLeft, emptySProductionNode, leafRight
            }, _location);
        // (S -> (S)(S)(S))
        var expectedRoot =
            new ParseTreeNode('S'.ToCategory(), production, new IParseTree[]
            {
                leafLeft, emptySProductionNode, leafRight,
                leafLeft, expectedMiddleNode, leafRight,
                leafLeft, emptySProductionNode, leafRight
            }, _location);

        Assert.Equal(expectedRoot, result);
    }

    /* S -> S*
     * result: error
     * could be S -> SS or S -> S -> SS etc...
     */
    [Fact]
    public void AmbiguousGrammar()
    {
        var production = new Production('S'.ToCategory(), Regex.Star(Regex.Atom('S'.ToCategory())));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production
        });
        Assert.Throws<NotSLRGrammarException>(() => Parser.FromGrammar(grammar, '\0'.ToCategory()));
    }

    /* This grammar is not SLR
     * S -> Aa
     * S -> bAc
     * S -> dc
     * S -> bda
     * A -> d
     * source: https://people.cs.vt.edu/~ryder/515/f05/homework/hw1ans.pdf
     * result: error
     */
    [Fact]
    public void NotSlrGrammar()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Concat(
            Regex.Atom('A'.ToCategory()),
            Regex.Atom('a'.ToCategory())
        ));
        var production2 = new Production('S'.ToCategory(), Regex.Concat(
            Regex.Atom('b'.ToCategory()),
            Regex.Atom('A'.ToCategory()),
            Regex.Atom('c'.ToCategory())
        ));
        var production3 = new Production('S'.ToCategory(), Regex.Concat(
            Regex.Atom('d'.ToCategory()),
            Regex.Atom('c'.ToCategory())
        ));
        var production4 = new Production('S'.ToCategory(), Regex.Concat(
            Regex.Atom('b'.ToCategory()),
            Regex.Atom('d'.ToCategory()),
            Regex.Atom('a'.ToCategory())
        ));
        var production5 = new Production('A'.ToCategory(), Regex.Concat(
            Regex.Atom('d'.ToCategory())
        ));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1,
            production2,
            production3,
            production4,
            production5
        });
        Assert.Throws<NotSLRGrammarException>(() => Parser.FromGrammar(grammar, '\0'.ToCategory()));
    }

    /* if (x) {} [else {}] y = 1
     * S -> IA
     * I -> ibE
     * E -> Eps|eb
     * A -> a
     * ibeba
     */
    [Fact]
    public void IfElseGrammar()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Concat(
            Regex.Atom('I'.ToCategory()),
            Regex.Atom('A'.ToCategory())
        ));
        var production2 = new Production('I'.ToCategory(), Regex.Concat(
            Regex.Atom('i'.ToCategory()),
            Regex.Atom('b'.ToCategory()),
            Regex.Atom('E'.ToCategory())
        ));
        var production3 = new Production('E'.ToCategory(), Regex.Epsilon);
        var production4 = new Production('E'.ToCategory(), Regex.Concat(
            Regex.Atom('e'.ToCategory()),
            Regex.Atom('b'.ToCategory())
        ));
        var production5 = new Production('A'.ToCategory(), Regex.Concat(
            Regex.Atom('a'.ToCategory())
        ));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1,
            production2,
            production3,
            production4,
            production5
        });
        var parser = Parser.FromGrammar(grammar, '\0'.ToCategory());

        var leaf = new ParseTreeLeaf('S'.ToCategory(), _location);
        var leaves = new[] { leaf };

        var result = parser.Process(leaves, new Mock<IDiagnostics>().Object);

        Assert.Equal(leaf, result);
    }
}
