namespace sernickTest.Grammar.Dfa;

using sernick.Common.Dfa;
using sernick.Grammar.Dfa;
using sernickTest.Tokenizer.Lexer.Helpers;

public class TestGrammarAnalysis
{
    [Fact(Skip = "GrammarAnalysis methods not implemented")]
    public void TestSimple()
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
            { (0, 'A'), 1 },
            { (1, 'B'), 2 },
            { (2, 'A'), 3 },
        };
        var dfa3 = new FakeDfa(transitions3, 0, new HashSet<int> { 3 });

        // A -> eps
        // B -> A
        // C -> ABA
        var productions = new Dictionary<char, IDfaWithConfig<int>> {
            { 'A', dfa1 },
            { 'B', dfa2 },
            { 'C', dfa3 }
        };
        var grammar = new DfaGrammar<char, int>('C', productions);

        var nullable = GrammarAnalysis.Nullable(grammar);
        var expectedNullable = new HashSet<char> { 'A', 'B', 'C' };
        Assert.True(expectedNullable.SetEquals(nullable));

        var first = GrammarAnalysis.First(grammar, nullable);
        var expectedFirst = new Dictionary<char, HashSet<char>>
        {
            {'A', new HashSet<char>{'A'}},
            {'B', new HashSet<char>{'A','B'}},
            {'C', new HashSet<char>{'A','C'}}
        };
        foreach (var (symbol, expectedSet) in expectedFirst)
        {
            Assert.True(expectedSet.SetEquals(first[symbol]));
        }

        var follow = GrammarAnalysis.Follow(grammar, nullable, first);
        var expectedFollow = new Dictionary<char, HashSet<char>>
        {
            {'A', new HashSet<char>{'A','B'}},
            {'B', new HashSet<char>{'A'}},
            {'C', new HashSet<char>{}}
        };
        foreach (var (symbol, expectedSet) in expectedFollow)
        {
            Assert.True(expectedSet.SetEquals(follow[symbol]));
        }
    }
}
