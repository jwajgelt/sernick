namespace sernickTest.Parser;

using Diagnostics;
using Helpers;
using Input;
using sernick.Parser;

using Parser = sernick.Parser.Parser<Helpers.CharCategory, Helpers.CharCategory>;
using Regex = sernick.Common.Regex.Regex<Helpers.CharCategory>;
using Grammar = sernick.Grammar.Syntax.Grammar<Helpers.CharCategory>;
using IParseTree = sernick.Parser.ParseTree.IParseTree<Helpers.CharCategory>;
using ParseTreeNode = sernick.Parser.ParseTree.ParseTreeNode<Helpers.CharCategory>;
using ParseTreeLeaf = sernick.Parser.ParseTree.ParseTreeLeaf<Helpers.CharCategory>;
using Production = sernick.Grammar.Syntax.Production<Helpers.CharCategory>;

public class ParserTest
{
    // Checked sequence is equal to start symbol
    [Fact(Skip = "Parser constructor not implemented")]
    public void EmptyGrammarInstantlyAccepting()
    {
        var grammar = new Grammar('S'.ToCategory(), Array.Empty<Production>());
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('S'.ToCategory(), location, location);
        var leaves = new[] { leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(leaves, diagnostics);
        
        Assert.Equal(leaf, result);
    }

    // Checked sequence is different from start symbol
    [Fact(Skip = "Parser constructor not implemented")]
    public void EmptyGrammarInstantlyRejecting()
    {
        var grammar = new Grammar('S'.ToCategory(), Array.Empty<Production>());
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('A'.ToCategory(), location, location);
        var leaves = new[] { leaf };

        var diagnostics = new FakeDiagnostics();

        Assert.Throws<ParsingException>(() => parser.Process(leaves, diagnostics));
    }

    /*  S -> A
     * input: A
     * result:
     *   S
     *   |(S -> A)
     *   A
     */
    [Fact(Skip = "Parser constructor not implemented")]
    public void OneAtomProductionGrammarCorrectInput()
    {
        var production = new Production('S'.ToCategory(), Regex.Atom('A'.ToCategory()));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production
        });
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('A'.ToCategory(), location, location);
        var leaves = new[] { leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(leaves, diagnostics);

        var expectedRoot = new ParseTreeNode('S'.ToCategory(), location, location, production, new[] { leaf });

        Assert.Equal(expectedRoot, result);
    }

    /*  S -> A
     * input: B
     * result: error
     */
    [Fact(Skip = "Parser constructor not implemented")]
    public void OneAtomProductionGrammarIncorrectInput()
    {
        var production = new Production('S'.ToCategory(), Regex.Atom('A'.ToCategory()));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production
        });
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('B'.ToCategory(), location, location);
        var leaves = new[] { leaf };

        var diagnostics = new FakeDiagnostics();

        Assert.Throws<ParsingException>(() => parser.Process(leaves, diagnostics));
    }

    /*  S -> A*
     * input: AA
     * result:
     *   S
     *   |(S -> A*)
     *   AA
     */
    [Fact(Skip = "Parser constructor not implemented")]
    public void OneStarProductionGrammarCorrectInput()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Star(Regex.Atom('A'.ToCategory())));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('A'.ToCategory(), location, location);
        var leaves = new[] { leaf, leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(leaves, diagnostics);

        Assert.True(result is ParseTreeNode);
        var root = result as ParseTreeNode;

        Assert.Equal('S', result.Symbol.Character);
        Assert.Equal(production1, root!.Production);
        Assert.Equal(2, root.Children.Count());

        var children = root.Children.ToArray();
        var child1 = children[0];
        var child2 = children[1];
        Assert.True(child1 is ParseTreeLeaf);
        Assert.True(child2 is ParseTreeLeaf);
        Assert.Equal('A', child1.Symbol.Character);
        Assert.Equal('A', child2.Symbol.Character);
    }

    /*  S -> A*
     * input: B
     * result: error
     */
    [Fact(Skip = "Parser constructor not implemented")]
    public void OneStarProductionGrammarIncorrectInput()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Star(Regex.Atom('A'.ToCategory())));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('B'.ToCategory(), location, location);
        var leaves = new[] { leaf };

        var diagnostics = new FakeDiagnostics();

        Assert.Throws<ParsingException>(() => parser.Process(leaves, diagnostics));
    }

    /*  S -> AB
     * input: AB
     * result:
     *   S
     *   |(S -> AB)
     *   AB
     */
    [Fact(Skip = "Parser constructor not implemented")]
    public void OneConcatProductionGrammarCorrectInput()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Concat(
            Regex.Atom('A'.ToCategory()),
            Regex.Atom('B'.ToCategory())
        ));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf1 = new ParseTreeLeaf('A'.ToCategory(), location, location);
        var leaf2 = new ParseTreeLeaf('B'.ToCategory(), location, location);
        var leaves = new[] { leaf1, leaf2 };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(leaves, diagnostics);

        Assert.True(result is ParseTreeNode);
        var root = result as ParseTreeNode;

        Assert.Equal('S', result.Symbol.Character);
        Assert.Equal(production1, root!.Production);
        Assert.Equal(2, root.Children.Count());

        var children = root.Children.ToArray();
        var child1 = children[0];
        var child2 = children[1];
        Assert.True(child1 is ParseTreeLeaf);
        Assert.True(child2 is ParseTreeLeaf);
        Assert.Equal('A', child1.Symbol.Character);
        Assert.Equal('B', child2.Symbol.Character);
    }

    /*  S -> AB
     * input: A
     * result: error
     */
    [Fact(Skip = "Parser constructor not implemented")]
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
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf1 = new ParseTreeLeaf('A'.ToCategory(), location, location);
        var leaves = new[] { leaf1 };

        var diagnostics = new FakeDiagnostics();

        Assert.Throws<ParsingException>(() => parser.Process(leaves, diagnostics));
    }

    /*  S -> A+B
     * input: B
     * result:
     *   S
     *   |(S -> A+B)
     *   B
     */
    [Fact(Skip = "Parser constructor not implemented")]
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
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('B'.ToCategory(), location, location);
        var leaves = new[] { leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(leaves, diagnostics);

        Assert.True(result is ParseTreeNode);
        var root = result as ParseTreeNode;

        Assert.Equal('S', result.Symbol.Character);
        Assert.Equal(production, root!.Production);
        Assert.Single(root.Children);

        var child = root.Children.First();
        Assert.True(child is ParseTreeLeaf);
        Assert.Equal('B', child!.Symbol.Character);
    }

    /*  S -> A+B
    * input: C
    * result: error
    */
    [Fact(Skip = "Parser constructor not implemented")]
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
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('C'.ToCategory(), location, location);
        var leaves = new[] { leaf };

        var diagnostics = new FakeDiagnostics();

        Assert.Throws<ParsingException>(() => parser.Process(leaves, diagnostics));
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
    [Fact(Skip = "Parser constructor not implemented")]
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
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('D'.ToCategory(), location, location);
        var leaves = new[] { leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(leaves, diagnostics);

        Assert.True(result is ParseTreeNode);
        var node0 = result as ParseTreeNode;
        Assert.Equal('S', node0!.Symbol.Character);
        Assert.Equal(production1, node0.Production);
        Assert.Single(node0.Children);

        var child1 = node0.Children.First();
        Assert.True(child1 is ParseTreeNode);
        var node1 = child1 as ParseTreeNode;
        Assert.Equal('A', node1!.Symbol.Character);
        Assert.Equal(production2, node1.Production);
        Assert.Single(node1.Children);

        var child2 = node1.Children.First();
        Assert.True(child2 is ParseTreeNode);
        var node2 = child2 as ParseTreeNode;
        Assert.Equal('B', node2!.Symbol.Character);
        Assert.Equal(production3, node2.Production);
        Assert.Single(node2.Children);

        var child3 = node2.Children.First();
        Assert.True(child3 is ParseTreeNode);
        var node3 = child3 as ParseTreeNode;
        Assert.Equal('C', node3!.Symbol.Character);
        Assert.Equal(production4, node3.Production);
        Assert.Single(node3.Children);

        var child4 = node3.Children.First();
        Assert.True(child4 is ParseTreeLeaf);
        Assert.Equal('D', child4!.Symbol.Character);
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
    [Fact(Skip = "Parser constructor not implemented")]
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
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('B'.ToCategory(), location, location);
        var leaves = new[] { leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(leaves, diagnostics);

        Assert.True(result is ParseTreeNode);
        var node0 = result as ParseTreeNode;
        Assert.Equal('S', node0!.Symbol.Character);
        Assert.Equal(production1, node0.Production);
        Assert.Single(node0.Children);

        var child1 = node0.Children.First();
        Assert.True(child1 is ParseTreeNode);
        var node1 = child1 as ParseTreeNode;
        Assert.Equal('A', node1!.Symbol.Character);
        Assert.Equal(production2, node1.Production);
        Assert.Single(node1.Children);

        var child2 = node1.Children.First();
        Assert.True(child2 is ParseTreeLeaf);
        Assert.Equal('B', child2.Symbol.Character);
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
    [Fact(Skip = "Parser constructor not implemented")]
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
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf = new ParseTreeLeaf('A'.ToCategory(), location, location);
        var leaves = new[] { leaf, leaf, leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(leaves, diagnostics);

        Assert.True(result is ParseTreeNode);
        var node0 = result as ParseTreeNode;
        Assert.Equal('S', node0!.Symbol.Character);
        Assert.Equal(production2, node0.Production);
        Assert.Equal(2, node0.Children.Count());

        // first production results
        var children1 = node0.Children.ToArray();
        var child1_1 = children1[1];
        Assert.True(child1_1 is ParseTreeLeaf);
        Assert.Equal('A', child1_1.Symbol.Character);

        var child1_0 = children1[0];
        Assert.True(child1_0 is ParseTreeNode);
        var node1_0 = child1_0 as ParseTreeNode;
        Assert.Equal('S', node1_0!.Symbol.Character);
        Assert.Equal(production2, node1_0.Production);
        Assert.Equal(2, node1_0.Children.Count());

        // second production results
        var children2 = node1_0.Children.ToArray();
        var child2_1 = children2[1];
        Assert.True(child2_1 is ParseTreeLeaf);
        Assert.Equal('A', child2_1.Symbol.Character);

        var child2_0 = children2[0];
        Assert.True(child2_0 is ParseTreeNode);
        var node2_0 = child1_0 as ParseTreeNode;
        Assert.Equal('S', node2_0!.Symbol.Character);
        Assert.Equal(production1, node2_0.Production);
        Assert.Single(node2_0.Children);

        // last production result
        var child3 = node2_0.Children.First();
        Assert.True(child3 is ParseTreeLeaf);
        Assert.Equal('A', child3.Symbol.Character);
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
    [Fact(Skip = "Parser constructor not implemented")]
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
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();
        var leaf1 = new ParseTreeLeaf('P'.ToCategory(), location, location);
        var leaf2 = new ParseTreeLeaf('a'.ToCategory(), location, location);
        var leaf3 = new ParseTreeLeaf('r'.ToCategory(), location, location);
        var leaf4 = new ParseTreeLeaf('s'.ToCategory(), location, location);
        var leaf5 = new ParseTreeLeaf('e'.ToCategory(), location, location);
        var leaf6 = new ParseTreeLeaf('r'.ToCategory(), location, location);

        var leaves = new[] { leaf1, leaf2, leaf3, leaf4, leaf5, leaf6 };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(leaves, diagnostics);

        // root (S -> SX)
        Assert.True(result is ParseTreeNode);
        var node0 = result as ParseTreeNode;
        Assert.Equal('S', node0!.Symbol.Character);
        Assert.Equal(production2, node0.Production);
        Assert.Equal(2, node0.Children.Count());
        var children1 = node0.Children.ToArray();

        // left branch (S -> X)
        var leftChild1 = children1[0];
        Assert.True(leftChild1 is ParseTreeNode);
        var leftNode1 = leftChild1 as ParseTreeNode;
        Assert.Equal('S', leftChild1.Symbol.Character);
        Assert.Equal(production1, leftNode1!.Production);
        Assert.Single(leftNode1.Children);

        // (X -> Yr)
        var leftChild2 = leftNode1.Children.First();
        Assert.True(leftChild2 is ParseTreeNode);
        var leftNode2 = leftChild2 as ParseTreeNode;
        Assert.Equal('X', leftChild2.Symbol.Character);
        Assert.Equal(production3, leftNode2!.Production);
        Assert.Equal(2, leftNode2.Children.Count());
        var leftChildren3 = leftNode2.Children.ToArray();

        // r
        var leftChild3_1 = leftChildren3[1];
        Assert.True(leftChild3_1 is ParseTreeLeaf);
        Assert.Equal('r', leftChild3_1.Symbol.Character);

        // (Y -> Pa)
        var leftChild3_0 = leftChildren3[0];
        Assert.True(leftChild3_0 is ParseTreeNode);
        var leftNode3_0 = leftChild3_0 as ParseTreeNode;
        Assert.Equal('Y', leftChild3_0.Symbol.Character);
        Assert.Equal(production4, leftNode3_0!.Production);
        Assert.Equal(2, leftNode3_0.Children.Count());
        var leftChildren4 = leftNode3_0.Children.ToArray();

        // P
        var leftChild4_0 = leftChildren4[0];
        Assert.True(leftChild4_0 is ParseTreeLeaf);
        Assert.Equal('P', leftChild4_0.Symbol.Character);

        // a
        var leftChild4_1 = leftChildren4[1];
        Assert.True(leftChild4_1 is ParseTreeLeaf);
        Assert.Equal('a', leftChild4_1.Symbol.Character);

        // right branch (X -> Yr)
        var rightChild1 = children1[1];
        Assert.True(rightChild1 is ParseTreeNode);
        var rightNode1 = rightChild1 as ParseTreeNode;
        Assert.Equal('X', rightChild1.Symbol.Character);
        Assert.Equal(production3, rightNode1!.Production);
        Assert.Equal(2, rightNode1.Children.Count());
        var rightChildren2 = rightNode1.Children.ToArray();

        // r
        var rightChild2_1 = rightChildren2[1];
        Assert.True(rightChild2_1 is ParseTreeLeaf);
        Assert.Equal('r', rightChild2_1.Symbol.Character);

        // (Y -> se)
        var rightChild2_0 = rightChildren2[0];
        Assert.True(rightChild2_0 is ParseTreeNode);
        var rightNode2_0 = rightChild2_0 as ParseTreeNode;
        Assert.Equal('Y', rightChild2_0.Symbol.Character);
        Assert.Equal(production5, rightNode2_0!.Production);
        Assert.Equal(2, rightNode2_0.Children.Count());
        var rightChildren3 = rightNode2_0.Children.ToArray();

        // s
        var rightChild3_0 = rightChildren3[0];
        Assert.True(rightChild3_0 is ParseTreeLeaf);
        Assert.Equal('s', rightChild3_0.Symbol.Character);

        // e
        var rightChild3_1 = rightChildren3[1];
        Assert.True(rightChild3_1 is ParseTreeLeaf);
        Assert.Equal('e', rightChild3_1.Symbol.Character);
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
    [Fact(Skip = "Parser constructor not implemented")]
    public void ComplexGrammarCorrectInputCase2()
    {
        var production = new Production('S'.ToCategory(), Regex.Star(
            Regex.Concat(
                Regex.Atom('('.ToCategory()),
                Regex.Atom('S'.ToCategory()),
                Regex.Atom(')'.ToCategory())
            )));
        var grammar = new Grammar('S'.ToCategory(), new[] { production });
        var parser = Parser.FromGrammar(grammar);

        var location = new FakeLocation();

        // ()(()())()
        var leafLeft = new ParseTreeLeaf('('.ToCategory(), location, location);
        var leafRight = new ParseTreeLeaf(')'.ToCategory(), location, location);

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

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(leaves, diagnostics);

        // root (S -> (S)(S)(S))
        Assert.True(result is ParseTreeNode);
        var node0 = result as ParseTreeNode;
        Assert.Equal('S', node0!.Symbol.Character);
        Assert.Equal(production, node0.Production);
        Assert.Equal(9, node0.Children.Count());
        var children1 = node0.Children.ToArray();

        // left (S) (S -> eps)
        CheckEmptyParenthesis(children1[0], children1[1], children1[2], production);

        // right (S) (S -> eps)
        CheckEmptyParenthesis(children1[6], children1[7], children1[8], production);

        // middle (S -> (S)(S))
        var child1_3 = children1[3];
        var child1_4 = children1[4];
        var child1_5 = children1[5];

        // (
        Assert.True(child1_3 is ParseTreeLeaf);
        Assert.Equal('(', child1_3.Symbol.Character);

        // )
        Assert.True(child1_5 is ParseTreeLeaf);
        Assert.Equal(')', child1_5.Symbol.Character);

        // (S -> (S)(S))
        Assert.True(child1_4 is ParseTreeNode);
        var node1_4 = child1_4 as ParseTreeNode;
        Assert.Equal('S', node1_4!.Symbol.Character);
        Assert.Equal(production, node1_4.Production);
        Assert.Equal(6, node1_4.Children.Count());
        var children2 = node1_4.Children.ToArray();

        // left (S) (S -> eps)
        CheckEmptyParenthesis(children2[0], children2[1], children2[2], production);

        // right (S) (S -> eps)
        CheckEmptyParenthesis(children2[3], children2[4], children2[5], production);
    }

    private static void CheckEmptyParenthesis(IParseTree left, IParseTree middle,
        IParseTree right, Production production)
    {
        // (
        Assert.True(left is ParseTreeLeaf);
        Assert.Equal('(', left.Symbol.Character);

        // )
        Assert.True(right is ParseTreeLeaf);
        Assert.Equal(')', right.Symbol.Character);

        // (S -> eps)
        Assert.True(middle is ParseTreeNode);
        var node = middle as ParseTreeNode;
        Assert.Equal('S', node!.Symbol.Character);
        Assert.Equal(production, node.Production);
        Assert.Empty(node.Children);
    }

    /* S -> S*
     * result: error
     * could be S -> SS or S -> S -> SS etc...
     */
    [Fact(Skip = "Parser constructor not implemented")]
    public void AmbiguousGrammar()
    {
        var production1 = new Production('S'.ToCategory(), Regex.Star(Regex.Atom('S'.ToCategory())));
        var grammar = new Grammar('S'.ToCategory(), new[]
        {
            production1
        });
        Assert.Throws<NotSLRGrammarException>(() => Parser.FromGrammar(grammar));
    }

    /* This grammar is not SLR
     * S -> Aa
     * S -> bAc
     * S -> dc
     * S -> bda
     * A -> a
     * source: https://people.cs.vt.edu/~ryder/515/f05/homework/hw1ans.pdf
     * result: error
     */
    [Fact(Skip = "Parser constructor not implemented")]
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
        Assert.Throws<NotSLRGrammarException>(() => Parser.FromGrammar(grammar));
    }
}
