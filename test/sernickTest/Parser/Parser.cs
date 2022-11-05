namespace sernickTest.Parser;

using Diagnostics;
using Input;
using sernick.Common.Regex;
using sernick.Grammar.Syntax;
using sernick.Parser;
using sernick.Parser.ParseTree;

public class ParserTest
{
    // Checked sequence is equal to start symbol
    [Fact(Skip = "Parser constructor not implemented")]
    public void EmptyGrammarInstantlyAccepting()
    {
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), Array.Empty<Production<CharCategory>>());
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('S'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(tree, diagnostics);

        Assert.True(result is ParseTreeLeaf<CharCategory>);
        Assert.Equal('S', result.Symbol.Character);
    }

    // Checked sequence is different from start symbol
    [Fact(Skip = "Parser constructor not implemented")]
    public void EmptyGrammarInstantlyRejecting()
    {
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), Array.Empty<Production<CharCategory>>());
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('A'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf };

        var diagnostics = new FakeDiagnostics();

        Assert.Throws<ParsingException>(() => parser.Process(tree, diagnostics));
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Atom('A'.ToCategory()));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('A'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(tree, diagnostics);

        Assert.True(result is ParseTreeNode<CharCategory>);
        var root = result as ParseTreeNode<CharCategory>;

        Assert.Equal('S', result.Symbol.Character);
        Assert.Equal(production1, root!.Production);
        Assert.Single(root.Children);

        var child = root.Children.First();
        Assert.True(child is ParseTreeLeaf<CharCategory>);
        Assert.Equal('A', child.Symbol.Character);
    }

    /*  S -> A
     * input: B
     * result: error
     */
    [Fact(Skip = "Parser constructor not implemented")]
    public void OneAtomProductionGrammarIncorrectInput()
    {
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Atom('A'.ToCategory()));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('B'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf };

        var diagnostics = new FakeDiagnostics();

        Assert.Throws<ParsingException>(() => parser.Process(tree, diagnostics));
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Star(Regex<CharCategory>.Atom('A'.ToCategory())));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('A'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf, leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(tree, diagnostics);

        Assert.True(result is ParseTreeNode<CharCategory>);
        var root = result as ParseTreeNode<CharCategory>;

        Assert.Equal('S', result.Symbol.Character);
        Assert.Equal(production1, root!.Production);
        Assert.Equal(2, root.Children.Count());

        var children = root.Children.ToArray();
        var child1 = children[0];
        var child2 = children[1];
        Assert.True(child1 is ParseTreeLeaf<CharCategory>);
        Assert.True(child2 is ParseTreeLeaf<CharCategory>);
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Star(Regex<CharCategory>.Atom('A'.ToCategory())));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('B'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf };

        var diagnostics = new FakeDiagnostics();

        Assert.Throws<ParsingException>(() => parser.Process(tree, diagnostics));
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('A'.ToCategory()),
            Regex<CharCategory>.Atom('B'.ToCategory())
        ));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf1 = new ParseTreeLeaf<CharCategory>('A'.ToCategory(), fakeLocation, fakeLocation);
        var leaf2 = new ParseTreeLeaf<CharCategory>('B'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf1, leaf2 };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(tree, diagnostics);

        Assert.True(result is ParseTreeNode<CharCategory>);
        var root = result as ParseTreeNode<CharCategory>;

        Assert.Equal('S', result.Symbol.Character);
        Assert.Equal(production1, root!.Production);
        Assert.Equal(2, root.Children.Count());

        var children = root.Children.ToArray();
        var child1 = children[0];
        var child2 = children[1];
        Assert.True(child1 is ParseTreeLeaf<CharCategory>);
        Assert.True(child2 is ParseTreeLeaf<CharCategory>);
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('A'.ToCategory()),
            Regex<CharCategory>.Atom('B'.ToCategory())
        ));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf1 = new ParseTreeLeaf<CharCategory>('A'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf1 };

        var diagnostics = new FakeDiagnostics();

        Assert.Throws<ParsingException>(() => parser.Process(tree, diagnostics));
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Union(
            Regex<CharCategory>.Atom('A'.ToCategory()),
            Regex<CharCategory>.Atom('B'.ToCategory())
        ));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('B'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(tree, diagnostics);

        Assert.True(result is ParseTreeNode<CharCategory>);
        var root = result as ParseTreeNode<CharCategory>;

        Assert.Equal('S', result.Symbol.Character);
        Assert.Equal(production1, root!.Production);
        Assert.Single(root.Children);

        var child = root.Children.First();
        Assert.True(child is ParseTreeLeaf<CharCategory>);
        Assert.Equal('B', child!.Symbol.Character);
    }

    /*  S -> A+B
    * input: C
    * result: error
    */
    [Fact(Skip = "Parser constructor not implemented")]
    public void OneUnionProductionGrammarIncorrectInput()
    {
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Union(
            Regex<CharCategory>.Atom('A'.ToCategory()),
            Regex<CharCategory>.Atom('B'.ToCategory())
        ));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('C'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf };

        var diagnostics = new FakeDiagnostics();

        Assert.Throws<ParsingException>(() => parser.Process(tree, diagnostics));
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Atom('A'.ToCategory()));
        var production2 = new Production<CharCategory>('A'.ToCategory(), Regex<CharCategory>.Atom('B'.ToCategory()));
        var production3 = new Production<CharCategory>('B'.ToCategory(), Regex<CharCategory>.Atom('C'.ToCategory()));
        var production4 = new Production<CharCategory>('C'.ToCategory(), Regex<CharCategory>.Atom('D'.ToCategory()));

        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1,
            production2,
            production3,
            production4
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('D'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(tree, diagnostics);

        Assert.True(result is ParseTreeNode<CharCategory>);
        var node0 = result as ParseTreeNode<CharCategory>;
        Assert.Equal('S', node0!.Symbol.Character);
        Assert.Equal(production1, node0.Production);
        Assert.Single(node0.Children);

        var child1 = node0.Children.First();
        Assert.True(child1 is ParseTreeNode<CharCategory>);
        var node1 = child1 as ParseTreeNode<CharCategory>;
        Assert.Equal('A', node1!.Symbol.Character);
        Assert.Equal(production2, node1.Production);
        Assert.Single(node1.Children);

        var child2 = node1.Children.First();
        Assert.True(child2 is ParseTreeNode<CharCategory>);
        var node2 = child2 as ParseTreeNode<CharCategory>;
        Assert.Equal('B', node2!.Symbol.Character);
        Assert.Equal(production3, node2.Production);
        Assert.Single(node2.Children);

        var child3 = node2.Children.First();
        Assert.True(child3 is ParseTreeNode<CharCategory>);
        var node3 = child3 as ParseTreeNode<CharCategory>;
        Assert.Equal('C', node3!.Symbol.Character);
        Assert.Equal(production4, node3.Production);
        Assert.Single(node3.Children);

        var child4 = node3.Children.First();
        Assert.True(child4 is ParseTreeLeaf<CharCategory>);
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Atom('A'.ToCategory()));
        var production2 = new Production<CharCategory>('A'.ToCategory(), Regex<CharCategory>.Atom('B'.ToCategory()));
        var production3 = new Production<CharCategory>('B'.ToCategory(), Regex<CharCategory>.Atom('C'.ToCategory()));
        var production4 = new Production<CharCategory>('C'.ToCategory(), Regex<CharCategory>.Atom('D'.ToCategory()));

        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1,
            production2,
            production3,
            production4
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('B'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(tree, diagnostics);

        Assert.True(result is ParseTreeNode<CharCategory>);
        var node0 = result as ParseTreeNode<CharCategory>;
        Assert.Equal('S', node0!.Symbol.Character);
        Assert.Equal(production1, node0.Production);
        Assert.Single(node0.Children);

        var child1 = node0.Children.First();
        Assert.True(child1 is ParseTreeNode<CharCategory>);
        var node1 = child1 as ParseTreeNode<CharCategory>;
        Assert.Equal('A', node1!.Symbol.Character);
        Assert.Equal(production2, node1.Production);
        Assert.Single(node1.Children);

        var child2 = node1.Children.First();
        Assert.True(child2 is ParseTreeLeaf<CharCategory>);
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Atom('A'.ToCategory()));
        var production2 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('S'.ToCategory()),
            Regex<CharCategory>.Atom('A'.ToCategory())
        ));

        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1,
            production2
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf = new ParseTreeLeaf<CharCategory>('A'.ToCategory(), fakeLocation, fakeLocation);
        var tree = new List<IParseTree<CharCategory>> { leaf, leaf, leaf };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(tree, diagnostics);

        Assert.True(result is ParseTreeNode<CharCategory>);
        var node0 = result as ParseTreeNode<CharCategory>;
        Assert.Equal('S', node0!.Symbol.Character);
        Assert.Equal(production2, node0.Production);
        Assert.Equal(2, node0.Children.Count());

        // first production results
        var children1 = node0.Children.ToArray();
        var child1_1 = children1[1];
        Assert.True(child1_1 is ParseTreeLeaf<CharCategory>);
        Assert.Equal('A', child1_1.Symbol.Character);

        var child1_0 = children1[0];
        Assert.True(child1_0 is ParseTreeNode<CharCategory>);
        var node1_0 = child1_0 as ParseTreeNode<CharCategory>;
        Assert.Equal('S', node1_0!.Symbol.Character);
        Assert.Equal(production2, node1_0.Production);
        Assert.Equal(2, node1_0.Children.Count());

        // second production results
        var children2 = node1_0.Children.ToArray();
        var child2_1 = children2[1];
        Assert.True(child2_1 is ParseTreeLeaf<CharCategory>);
        Assert.Equal('A', child2_1.Symbol.Character);

        var child2_0 = children2[0];
        Assert.True(child2_0 is ParseTreeNode<CharCategory>);
        var node2_0 = child1_0 as ParseTreeNode<CharCategory>;
        Assert.Equal('S', node2_0!.Symbol.Character);
        Assert.Equal(production1, node2_0.Production);
        Assert.Single(node2_0.Children);

        // last production result
        var child3 = node2_0.Children.First();
        Assert.True(child3 is ParseTreeLeaf<CharCategory>);
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Atom('X'.ToCategory()));
        var production2 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('S'.ToCategory()),
            Regex<CharCategory>.Atom('X'.ToCategory())
        ));
        var production3 = new Production<CharCategory>('X'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('Y'.ToCategory()),
            Regex<CharCategory>.Atom('r'.ToCategory())
        ));
        var production4 = new Production<CharCategory>('Y'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('P'.ToCategory()),
            Regex<CharCategory>.Atom('a'.ToCategory())
        ));
        var production5 = new Production<CharCategory>('Y'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('s'.ToCategory()),
            Regex<CharCategory>.Atom('e'.ToCategory())
        ));

        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1,
            production2,
            production3,
            production4,
            production5
        });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();
        var leaf1 = new ParseTreeLeaf<CharCategory>('P'.ToCategory(), fakeLocation, fakeLocation);
        var leaf2 = new ParseTreeLeaf<CharCategory>('a'.ToCategory(), fakeLocation, fakeLocation);
        var leaf3 = new ParseTreeLeaf<CharCategory>('r'.ToCategory(), fakeLocation, fakeLocation);
        var leaf4 = new ParseTreeLeaf<CharCategory>('s'.ToCategory(), fakeLocation, fakeLocation);
        var leaf5 = new ParseTreeLeaf<CharCategory>('e'.ToCategory(), fakeLocation, fakeLocation);
        var leaf6 = new ParseTreeLeaf<CharCategory>('r'.ToCategory(), fakeLocation, fakeLocation);

        var tree = new List<IParseTree<CharCategory>> { leaf1, leaf2, leaf3, leaf4, leaf5, leaf6 };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(tree, diagnostics);

        // root (S -> SX)
        Assert.True(result is ParseTreeNode<CharCategory>);
        var node0 = result as ParseTreeNode<CharCategory>;
        Assert.Equal('S', node0!.Symbol.Character);
        Assert.Equal(production2, node0.Production);
        Assert.Equal(2, node0.Children.Count());
        var children1 = node0.Children.ToArray();

        // left branch (S -> X)
        var leftChild1 = children1[0];
        Assert.True(leftChild1 is ParseTreeNode<CharCategory>);
        var leftNode1 = leftChild1 as ParseTreeNode<CharCategory>;
        Assert.Equal('S', leftChild1.Symbol.Character);
        Assert.Equal(production1, leftNode1!.Production);
        Assert.Single(leftNode1.Children);

        // (X -> Yr)
        var leftChild2 = leftNode1.Children.First();
        Assert.True(leftChild2 is ParseTreeNode<CharCategory>);
        var leftNode2 = leftChild2 as ParseTreeNode<CharCategory>;
        Assert.Equal('X', leftChild2.Symbol.Character);
        Assert.Equal(production3, leftNode2!.Production);
        Assert.Equal(2, leftNode2.Children.Count());
        var leftChildren3 = leftNode2.Children.ToArray();

        // r
        var leftChild3_1 = leftChildren3[1];
        Assert.True(leftChild3_1 is ParseTreeLeaf<CharCategory>);
        Assert.Equal('r', leftChild3_1.Symbol.Character);

        // (Y -> Pa)
        var leftChild3_0 = leftChildren3[0];
        Assert.True(leftChild3_0 is ParseTreeNode<CharCategory>);
        var leftNode3_0 = leftChild3_0 as ParseTreeNode<CharCategory>;
        Assert.Equal('Y', leftChild3_0.Symbol.Character);
        Assert.Equal(production4, leftNode3_0!.Production);
        Assert.Equal(2, leftNode3_0.Children.Count());
        var leftChildren4 = leftNode3_0.Children.ToArray();

        // P
        var leftChild4_0 = leftChildren4[0];
        Assert.True(leftChild4_0 is ParseTreeLeaf<CharCategory>);
        Assert.Equal('P', leftChild4_0.Symbol.Character);

        // a
        var leftChild4_1 = leftChildren4[1];
        Assert.True(leftChild4_1 is ParseTreeLeaf<CharCategory>);
        Assert.Equal('a', leftChild4_1.Symbol.Character);

        // right branch (X -> Yr)
        var rightChild1 = children1[1];
        Assert.True(rightChild1 is ParseTreeNode<CharCategory>);
        var rightNode1 = rightChild1 as ParseTreeNode<CharCategory>;
        Assert.Equal('X', rightChild1.Symbol.Character);
        Assert.Equal(production3, rightNode1!.Production);
        Assert.Equal(2, rightNode1.Children.Count());
        var rightChildren2 = rightNode1.Children.ToArray();

        // r
        var rightChild2_1 = rightChildren2[1];
        Assert.True(rightChild2_1 is ParseTreeLeaf<CharCategory>);
        Assert.Equal('r', rightChild2_1.Symbol.Character);

        // (Y -> se)
        var rightChild2_0 = rightChildren2[0];
        Assert.True(rightChild2_0 is ParseTreeNode<CharCategory>);
        var rightNode2_0 = rightChild2_0 as ParseTreeNode<CharCategory>;
        Assert.Equal('Y', rightChild2_0.Symbol.Character);
        Assert.Equal(production5, rightNode2_0!.Production);
        Assert.Equal(2, rightNode2_0.Children.Count());
        var rightChildren3 = rightNode2_0.Children.ToArray();

        // s
        var rightChild3_0 = rightChildren3[0];
        Assert.True(rightChild3_0 is ParseTreeLeaf<CharCategory>);
        Assert.Equal('s', rightChild3_0.Symbol.Character);

        // e
        var rightChild3_1 = rightChildren3[1];
        Assert.True(rightChild3_1 is ParseTreeLeaf<CharCategory>);
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
        var production = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Star(
            Regex<CharCategory>.Concat(
                Regex<CharCategory>.Atom('('.ToCategory()),
                Regex<CharCategory>.Atom('S'.ToCategory()),
                Regex<CharCategory>.Atom(')'.ToCategory())
            )));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[] { production });
        var parser = Parser<CharCategory, CharCategory>.FromGrammar(grammar);

        var fakeLocation = new FakeLocation();

        // ()(()())()
        var leaf1 = new ParseTreeLeaf<CharCategory>('('.ToCategory(), fakeLocation, fakeLocation);
        var leaf2 = new ParseTreeLeaf<CharCategory>(')'.ToCategory(), fakeLocation, fakeLocation);
        var leaf3 = new ParseTreeLeaf<CharCategory>('('.ToCategory(), fakeLocation, fakeLocation);
        var leaf4 = new ParseTreeLeaf<CharCategory>('('.ToCategory(), fakeLocation, fakeLocation);
        var leaf5 = new ParseTreeLeaf<CharCategory>(')'.ToCategory(), fakeLocation, fakeLocation);
        var leaf6 = new ParseTreeLeaf<CharCategory>('('.ToCategory(), fakeLocation, fakeLocation);
        var leaf7 = new ParseTreeLeaf<CharCategory>(')'.ToCategory(), fakeLocation, fakeLocation);
        var leaf8 = new ParseTreeLeaf<CharCategory>(')'.ToCategory(), fakeLocation, fakeLocation);
        var leaf9 = new ParseTreeLeaf<CharCategory>('('.ToCategory(), fakeLocation, fakeLocation);
        var leaf10 = new ParseTreeLeaf<CharCategory>(')'.ToCategory(), fakeLocation, fakeLocation);

        var tree = new List<IParseTree<CharCategory>>
        {
            leaf1,
            leaf2,
            leaf3,
            leaf4,
            leaf5,
            leaf6,
            leaf7,
            leaf8,
            leaf9,
            leaf10
        };

        var diagnostics = new FakeDiagnostics();
        var result = parser.Process(tree, diagnostics);

        // root (S -> (S)(S)(S))
        Assert.True(result is ParseTreeNode<CharCategory>);
        var node0 = result as ParseTreeNode<CharCategory>;
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
        Assert.True(child1_3 is ParseTreeLeaf<CharCategory>);
        Assert.Equal('(', child1_3.Symbol.Character);

        // )
        Assert.True(child1_5 is ParseTreeLeaf<CharCategory>);
        Assert.Equal(')', child1_5.Symbol.Character);

        // (S -> (S)(S))
        Assert.True(child1_4 is ParseTreeNode<CharCategory>);
        var node1_4 = child1_4 as ParseTreeNode<CharCategory>;
        Assert.Equal('S', node1_4!.Symbol.Character);
        Assert.Equal(production, node1_4.Production);
        Assert.Equal(6, node1_4.Children.Count());
        var children2 = node1_4.Children.ToArray();

        // left (S) (S -> eps)
        CheckEmptyParenthesis(children2[0], children2[1], children2[2], production);

        // right (S) (S -> eps)
        CheckEmptyParenthesis(children2[3], children2[4], children2[5], production);
    }

    private static void CheckEmptyParenthesis(IParseTree<CharCategory> left, IParseTree<CharCategory> middle,
        IParseTree<CharCategory> right, Production<CharCategory> production)
    {
        // (
        Assert.True(left is ParseTreeLeaf<CharCategory>);
        Assert.Equal('(', left.Symbol.Character);

        // )
        Assert.True(right is ParseTreeLeaf<CharCategory>);
        Assert.Equal(')', right.Symbol.Character);

        // (S -> eps)
        Assert.True(middle is ParseTreeNode<CharCategory>);
        var node = middle as ParseTreeNode<CharCategory>;
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Star(Regex<CharCategory>.Atom('S'.ToCategory())));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1
        });
        Assert.Throws<NotSLRGrammarException>(() => Parser<CharCategory, CharCategory>.FromGrammar(grammar));
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
        var production1 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('A'.ToCategory()),
            Regex<CharCategory>.Atom('a'.ToCategory())
        ));
        var production2 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('b'.ToCategory()),
            Regex<CharCategory>.Atom('A'.ToCategory()),
            Regex<CharCategory>.Atom('c'.ToCategory())
        ));
        var production3 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('d'.ToCategory()),
            Regex<CharCategory>.Atom('c'.ToCategory())
        ));
        var production4 = new Production<CharCategory>('S'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('b'.ToCategory()),
            Regex<CharCategory>.Atom('d'.ToCategory()),
            Regex<CharCategory>.Atom('a'.ToCategory())
        ));
        var production5 = new Production<CharCategory>('A'.ToCategory(), Regex<CharCategory>.Concat(
            Regex<CharCategory>.Atom('a'.ToCategory())
        ));
        var grammar = new Grammar<CharCategory>('S'.ToCategory(), new[]
        {
            production1,
            production2,
            production3,
            production4,
            production5
        });
        Assert.Throws<NotSLRGrammarException>(() => Parser<CharCategory, CharCategory>.FromGrammar(grammar));
    }
}
