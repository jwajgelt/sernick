namespace sernickTest.Grammar.Dfa;

using sernick.Common.Dfa;
using sernick.Grammar.Dfa;
using sernickTest.Tokenizer.Lexer.Helpers;

public class TestGrammarAnalysis
{
    // A -> eps
    // B -> b
    // C -> c|eps
    [Fact(Skip = "GrammarAnalysis methods not implemented")]
    public void NullableSimple()
    {
        var transitions1 = new Dictionary<(int, char), int> { };
        var dfa1 = new FakeDfa(transitions1, 0, new HashSet<int> { 0 });

        var transitions2 = new Dictionary<(int, char), int>
        {
            { (0, 'b'), 1 }
        };
        var dfa2 = new FakeDfa(transitions2, 0, new HashSet<int> { 1 });

        var transitions3 = new Dictionary<(int, char), int>
        {
            { (0, 'c'), 1 }
        };
        var dfa3 = new FakeDfa(transitions3, 0, new HashSet<int> { 0, 1 });

        var productions = new Dictionary<char, IDfaWithConfig<int, char>> {
            { 'A', dfa1 },
            { 'B', dfa2 },
            { 'C', dfa3 }
        };
        var grammar = new DfaGrammar<char, int>('S', productions);

        var expectedNullable = new HashSet<char> { 'A', 'C' };
        var expectedFirst = new Dictionary<char, HashSet<char>>
        {
            {'A', new HashSet<char>{'A'}},
            {'B', new HashSet<char>{'B','b'}},
            {'C', new HashSet<char>{'C','c'}},
            {'b', new HashSet<char>{'b'}},
            {'c', new HashSet<char>{'c'}}
        };
        var expectedFollow = new Dictionary<char, HashSet<char>>
        {
            {'A', new HashSet<char>{}},
            {'B', new HashSet<char>{}},
            {'C', new HashSet<char>{}},
            {'b', new HashSet<char>{}},
            {'c', new HashSet<char>{}}
        };
        Verify(grammar, expectedNullable, expectedFirst, expectedFollow);
    }

    // S -> ABA
    // A -> eps
    // B -> AA
    [Fact(Skip = "GrammarAnalysis methods not implemented")]
    public void NullableConditional()
    {
        var transitions1 = new Dictionary<(int, char), int>
        {
            { (0, 'A'), 1 },
            { (1, 'B'), 2 },
            { (2, 'A'), 3 },
        };
        var dfa1 = new FakeDfa(transitions1, 0, new HashSet<int> { 3 });

        var transitions2 = new Dictionary<(int, char), int> { };
        var dfa2 = new FakeDfa(transitions2, 0, new HashSet<int> { 0 });

        var transitions3 = new Dictionary<(int, char), int>
        {
            { (0, 'A'), 1 },
            { (1, 'A'), 2 }
        };
        var dfa3 = new FakeDfa(transitions3, 0, new HashSet<int> { 2 });

        var productions = new Dictionary<char, IDfaWithConfig<int, char>> {
            { 'S', dfa1 },
            { 'A', dfa2 },
            { 'B', dfa3 }
        };
        var grammar = new DfaGrammar<char, int>('S', productions);

        var expectedNullable = new HashSet<char> { 'S', 'A', 'B' };
        var expectedFirst = new Dictionary<char, HashSet<char>>
        {
            {'S', new HashSet<char>{'S','A','B'}},
            {'A', new HashSet<char>{'A'}},
            {'B', new HashSet<char>{'B','A'}}
        };
        var expectedFollow = new Dictionary<char, HashSet<char>>
        {
            {'S', new HashSet<char>{}},
            {'A', new HashSet<char>{'A','B'}},
            {'B', new HashSet<char>{'A'}}
        };
        Verify(grammar, expectedNullable, expectedFirst, expectedFollow);
    }

    // A -> eps
    // B -> b
    // S -> AAbB
    [Fact(Skip = "GrammarAnalysis methods not implemented")]
    public void FirstSimple()
    {
        var transitions1 = new Dictionary<(int, char), int> { };
        var dfa1 = new FakeDfa(transitions1, 0, new HashSet<int> { 0 });

        var transitions2 = new Dictionary<(int, char), int>
        {
            { (0, 'b'), 1 }
        };
        var dfa2 = new FakeDfa(transitions2, 0, new HashSet<int> { 1 });

        var transitions3 = new Dictionary<(int, char), int>
        {
            { (0, 'A'), 1 },
            { (1, 'A'), 2 },
            { (2, 'b'), 3 },
            { (3, 'B'), 4 }
        };
        var dfa3 = new FakeDfa(transitions3, 0, new HashSet<int> { 4 });

        var productions = new Dictionary<char, IDfaWithConfig<int, char>> {
            { 'A', dfa1 },
            { 'B', dfa2 },
            { 'S', dfa3 }
        };
        var grammar = new DfaGrammar<char, int>('S', productions);

        var expectedNullable = new HashSet<char> { 'A' };
        var expectedFirst = new Dictionary<char, HashSet<char>>
        {
            {'A', new HashSet<char>{'A'}},
            {'B', new HashSet<char>{'B','b'}},
            {'b', new HashSet<char>{'b'}},
            {'S', new HashSet<char>{'S','A','b'}}
        };
        var expectedFollow = new Dictionary<char, HashSet<char>>
        {
            {'A', new HashSet<char>{'A','b'}},
            {'B', new HashSet<char>{}},
            {'b', new HashSet<char>{'B','b'}},
            {'S', new HashSet<char>{}}
        };
        Verify(grammar, expectedNullable, expectedFirst, expectedFollow);
    }

    // A -> a
    // B -> C
    // C -> A
    // S -> B
    [Fact(Skip = "GrammarAnalysis methods not implemented")]
    public void FirstClosure()
    {
        var transitions1 = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 }
        };
        var dfa1 = new FakeDfa(transitions1, 0, new HashSet<int> { 1 });

        var transitions2 = new Dictionary<(int, char), int>
        {
            { (0, 'C'), 1 }
        };
        var dfa2 = new FakeDfa(transitions2, 0, new HashSet<int> { 1 });

        var transitions3 = new Dictionary<(int, char), int>
        {
            { (0, 'A'), 1 }
        };
        var dfa3 = new FakeDfa(transitions3, 0, new HashSet<int> { 1 });

        var transitions4 = new Dictionary<(int, char), int>
        {
            { (0, 'B'), 1 }
        };
        var dfa4 = new FakeDfa(transitions4, 0, new HashSet<int> { 1 });

        var productions = new Dictionary<char, IDfaWithConfig<int, char>> {
            { 'A', dfa1 },
            { 'B', dfa2 },
            { 'C', dfa3 },
            { 'S', dfa4 }
        };
        var grammar = new DfaGrammar<char, int>('S', productions);

        var expectedNullable = new HashSet<char> { };
        var expectedFirst = new Dictionary<char, HashSet<char>>
        {
            {'a', new HashSet<char>{'a'}},
            {'A', new HashSet<char>{'A','a'}},
            {'B', new HashSet<char>{'B','C','A','a'}},
            {'C', new HashSet<char>{'C','A','a'}},
            {'S', new HashSet<char>{'S','B','C','A','a'}}
        };
        var expectedFollow = new Dictionary<char, HashSet<char>>
        {
            {'a', new HashSet<char>{}},
            {'A', new HashSet<char>{}},
            {'B', new HashSet<char>{}},
            {'C', new HashSet<char>{}},
            {'S', new HashSet<char>{}}
        };
        Verify(grammar, expectedNullable, expectedFirst, expectedFollow);
    }

    // A -> a
    // B -> eps
    // C -> eps
    // S -> CABC
    [Fact(Skip = "GrammarAnalysis methods not implemented")]
    public void FollowSimple()
    {
        var transitions1 = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 }
        };
        var dfa1 = new FakeDfa(transitions1, 0, new HashSet<int> { 1 });

        var transitions2 = new Dictionary<(int, char), int> { };
        var dfa2 = new FakeDfa(transitions2, 0, new HashSet<int> { 0 });

        var transitions3 = new Dictionary<(int, char), int>
        {
            { (0, 'C'), 1 },
            { (1, 'A'), 2 },
            { (2, 'B'), 3 },
            { (3, 'C'), 4 }
        };
        var dfa3 = new FakeDfa(transitions3, 0, new HashSet<int> { 4 });

        var productions = new Dictionary<char, IDfaWithConfig<int, char>> {
            { 'A', dfa1 },
            { 'B', dfa2 },
            { 'C', dfa2 },
            { 'S', dfa3 }
        };
        var grammar = new DfaGrammar<char, int>('S', productions);

        var expectedNullable = new HashSet<char> { 'B', 'C' };
        var expectedFirst = new Dictionary<char, HashSet<char>>
        {
            {'a', new HashSet<char>{'a'}},
            {'A', new HashSet<char>{'A','a'}},
            {'B', new HashSet<char>{'B'}},
            {'C', new HashSet<char>{'C'}},
            {'S', new HashSet<char>{'S','A','a','C'}}
        };
        var expectedFollow = new Dictionary<char, HashSet<char>>
        {
            {'a', new HashSet<char>{'B','C'}},
            {'A', new HashSet<char>{'B','C'}},
            {'B', new HashSet<char>{'C'}},
            {'C', new HashSet<char>{'A','a'}},
            {'S', new HashSet<char>{}}
        };
        Verify(grammar, expectedNullable, expectedFirst, expectedFollow);
    }

    // A -> eps
    // B -> eps
    // C -> eps
    // D -> eps
    // E -> eps
    // S -> A(BB|CC)A(D|E) (with 2 accepting states)
    [Fact(Skip = "GrammarAnalysis methods not implemented")]
    public void FollowPaths()
    {
        var transitions1 = new Dictionary<(int, char), int> { };
        var dfa1 = new FakeDfa(transitions1, 0, new HashSet<int> { 0 });

        var transitions2 = new Dictionary<(int, char), int>
        {
            { (0, 'A'), 1 },
            { (1, 'B'), 2 },
            { (2, 'B'), 4 },
            { (1, 'C'), 3 },
            { (3, 'C'), 4 },
            { (4, 'A'), 5 },
            { (5, 'D'), 6 },
            { (5, 'E'), 7 }
        };
        var dfa2 = new FakeDfa(transitions2, 0, new HashSet<int> { 6, 7 });

        var productions = new Dictionary<char, IDfaWithConfig<int, char>> {
            { 'A', dfa1 },
            { 'B', dfa1 },
            { 'C', dfa1 },
            { 'D', dfa1 },
            { 'E', dfa1 },
            { 'S', dfa2 }
        };
        var grammar = new DfaGrammar<char, int>('S', productions);

        var expectedNullable = new HashSet<char> { 'A', 'B', 'C', 'D', 'E', 'S' };
        var expectedFirst = new Dictionary<char, HashSet<char>>
        {
            {'A', new HashSet<char>{'A'}},
            {'B', new HashSet<char>{'B'}},
            {'C', new HashSet<char>{'C'}},
            {'D', new HashSet<char>{'D'}},
            {'E', new HashSet<char>{'E'}},
            {'S', new HashSet<char>{'S','A','B','C','D','E'}}
        };
        var expectedFollow = new Dictionary<char, HashSet<char>>
        {
            {'A', new HashSet<char>{'A','B','C','D','E'}},
            {'B', new HashSet<char>{'A','B','D','E'}},
            {'C', new HashSet<char>{'A','C','D','E'}},
            {'D', new HashSet<char>{}},
            {'E', new HashSet<char>{}},
            {'S', new HashSet<char>{}}
        };
        Verify(grammar, expectedNullable, expectedFirst, expectedFollow);
    }

    // A -> a
    // B -> A*
    // C -> BA
    // D -> CA|B
    // S -> A(C|B)D
    [Fact(Skip = "GrammarAnalysis methods not implemented")]
    public void TestMixed()
    {
        var transitions1 = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 }
        };
        var dfa1 = new FakeDfa(transitions1, 0, new HashSet<int> { 1 });

        var transitions2 = new Dictionary<(int, char), int>
        {
            { (0, 'A'), 0 }
        };
        var dfa2 = new FakeDfa(transitions2, 0, new HashSet<int> { 0 });

        var transitions3 = new Dictionary<(int, char), int>
        {
            { (0, 'B'), 1 },
            { (1, 'A'), 2 }
        };
        var dfa3 = new FakeDfa(transitions3, 0, new HashSet<int> { 2 });

        var transitions4 = new Dictionary<(int, char), int>
        {
            { (0, 'C'), 1 },
            { (1, 'A'), 2 },
            { (0, 'B'), 2 }
        };
        var dfa4 = new FakeDfa(transitions4, 0, new HashSet<int> { 2 });

        var transitions5 = new Dictionary<(int, char), int>
        {
            { (0, 'A'), 1 },
            { (1, 'C'), 2 },
            { (1, 'B'), 2 },
            { (2, 'D'), 3 }
        };
        var dfa5 = new FakeDfa(transitions5, 0, new HashSet<int> { 3 });

        var productions = new Dictionary<char, IDfaWithConfig<int, char>> {
            { 'A', dfa1 },
            { 'B', dfa2 },
            { 'C', dfa3 },
            { 'D', dfa4 },
            { 'S', dfa5 }
        };
        var grammar = new DfaGrammar<char, int>('S', productions);

        var expectedNullable = new HashSet<char> { 'B', 'D' };
        var expectedFirst = new Dictionary<char, HashSet<char>>
        {
            {'a', new HashSet<char>{'a'}},
            {'A', new HashSet<char>{'A','a'}},
            {'B', new HashSet<char>{'B','A','a'}},
            {'C', new HashSet<char>{'C','B','A','a'}},
            {'D', new HashSet<char>{'D','C','B','A','a'}},
            {'S', new HashSet<char>{'S','A','a'}}
        };
        var expectedFollow = new Dictionary<char, HashSet<char>>
        {
            {'a', new HashSet<char>{'a','A','B','C','D'}},
            {'A', new HashSet<char>{'a','A','B','C','D'}},
            {'B', new HashSet<char>{'a','A','B','C','D'}},
            {'C', new HashSet<char>{'a','A','B','C','D'}},
            {'D', new HashSet<char>{}},
            {'S', new HashSet<char>{}}
        };
        Verify(grammar, expectedNullable, expectedFirst, expectedFollow);
    }

    // A -> eps
    // B -> A
    // C -> c
    // S -> (ABBA)*(AC|ABA)
    //      
    //   -> ( ) -A-> ( ) -C-> [ ]
    //       ^        B
    //       A        v
    //      ( ) <-B- ( ) -A-> [ ] 
    [Fact(Skip = "GrammarAnalysis methods not implemented")]
    public void TestLoop()
    {
        var transitions1 = new Dictionary<(int, char), int> { };
        var dfa1 = new FakeDfa(transitions1, 0, new HashSet<int> { 0 });

        var transitions2 = new Dictionary<(int, char), int>
        {
            { (0, 'A'), 1 }
        };
        var dfa2 = new FakeDfa(transitions2, 0, new HashSet<int> { 1 });

        var transitions3 = new Dictionary<(int, char), int>
        {
            { (0, 'c'), 1 }
        };
        var dfa3 = new FakeDfa(transitions3, 0, new HashSet<int> { 1 });

        var transitions4 = new Dictionary<(int, char), int>
        {
            { (0, 'A'), 1 },
            { (1, 'B'), 2 },
            { (2, 'B'), 3 },
            { (3, 'A'), 0 },
            { (1, 'C'), 4 },
            { (2, 'A'), 5 }
        };
        var dfa4 = new FakeDfa(transitions4, 0, new HashSet<int> { 4, 5 });

        var productions = new Dictionary<char, IDfaWithConfig<int, char>> {
            { 'A', dfa1 },
            { 'B', dfa2 },
            { 'C', dfa3 },
            { 'S', dfa4 }
        };
        var grammar = new DfaGrammar<char, int>('S', productions);

        var expectedNullable = new HashSet<char> { 'A', 'B', 'S' };
        var expectedFirst = new Dictionary<char, HashSet<char>>
        {
            {'A', new HashSet<char>{'A'}},
            {'B', new HashSet<char>{'B','A'}},
            {'C', new HashSet<char>{'C','c'}},
            {'c', new HashSet<char>{'c'}},
            {'S', new HashSet<char>{'S','A','B','C','c'}}
        };
        var expectedFollow = new Dictionary<char, HashSet<char>>
        {
            {'A', new HashSet<char>{'A','B','C','c'}},
            {'B', new HashSet<char>{'A','B','C','c'}},
            {'C', new HashSet<char>{}},
            {'c', new HashSet<char>{}},
            {'S', new HashSet<char>{'A','B','C','c'}}
        };
        Verify(grammar, expectedNullable, expectedFirst, expectedFollow);
    }

    private static void Verify(DfaGrammar<char, int> grammar,
        HashSet<char> expectedNullable,
        Dictionary<char, HashSet<char>> expectedFirst,
        Dictionary<char, HashSet<char>> expectedFollow)
    {
        var nullable = grammar.Nullable();
        Assert.Equal(expectedNullable.OrderBy(a => a), nullable.OrderBy(a => a));

        var first = grammar.First(nullable);
        foreach (var (symbol, expectedSet) in expectedFirst)
        {
            Assert.Equal(expectedSet.OrderBy(a => a), first[symbol].OrderBy(a => a));
        }

        var follow = grammar.Follow(nullable, first);
        foreach (var (symbol, expectedSet) in expectedFollow)
        {
            Assert.Equal(expectedSet.OrderBy(a => a), follow[symbol].OrderBy(a => a));
        }
    }
}
