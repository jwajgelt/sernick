namespace sernickTest.Tokenizer.Lexer;

using Helpers;
using sernick.Common.Dfa;
using sernick.Tokenizer;
using sernick.Tokenizer.Lexer;

public class Lexer
{
    [Fact]
    public void ReturnsMatching()
    {
        var categoryName = "cat";

        var transitions = new Dictionary<(int, char), int>
            {
                { (0, 'a'), 0}
            };
        var dfa = new FakeDfa(
            transitions,
            0,
            new HashSet<int> { 0 }
        );

        var input = new FakeInput("a");

        var categoryDfas = new Dictionary<string, IDfa<int, char>> { { categoryName, dfa } };

        var lexer = new Lexer<string, int>(categoryDfas);

        var result = lexer.Process(input, new NoOpDiagnostics()).ToList();
        Assert.Single(result, new Token<string>(categoryName, "a", input.Start, input.End));
    }

    [Fact]
    public void DoesNotReturnNonMatching()
    {
        var categoryName = "cat";

        var transitions = new Dictionary<(int, char), int>
            {
                { (0, 'a'), 0}
            };
        var dfa = new FakeDfa(
            transitions,
            0,
            new HashSet<int> { 0 }
        );

        var input = new FakeInput("b");

        var categoryDfas = new Dictionary<string, IDfa<int, char>> { { categoryName, dfa } };

        var lexer = new Lexer<string, int>(categoryDfas);

        var result = lexer.Process(input, new NoOpDiagnostics());
        Assert.Empty(result);
    }

    [Fact]
    public void MatchesTheLongestPossibleToken()
    {
        var categoryName = "cat";
        var transitions = new Dictionary<(int, char), int>
            {
                { (0, 'a'), 0 }
            };
        var dfa = new FakeDfa(
            transitions,
            0,
            new HashSet<int> { 0 }
        );

        var input = new FakeInput("aaaab");

        var categoryDfas = new Dictionary<string, IDfa<int, char>> { { categoryName, dfa } };

        var lexer = new Lexer<string, int>(categoryDfas);

        var result = lexer.Process(input, new NoOpDiagnostics());
        Assert.Single(result, new Token<string>(categoryName, "aaaa", input.Start, new FakeInput.Location(4)));
    }

    [Fact]
    public void MatchesMultipleTokens()
    {
        var categoryName = "cat";

        // this DFA matches `ab`
        var transitions = new Dictionary<(int, char), int>
            {
                { (0, 'a'), 1 },
                { (1, 'b'), 2 }
            };
        var dfa = new FakeDfa(
            transitions,
            0,
            new HashSet<int> { 2 }
        );

        var input = new FakeInput("abab");

        var categoryDfas = new Dictionary<string, IDfa<int, char>> { { categoryName, dfa } };

        var lexer = new Lexer<string, int>(categoryDfas);

        var result = lexer.Process(input, new NoOpDiagnostics());
        var expected = new List<Token<string>> {
            new(categoryName, "ab", input.Start, new FakeInput.Location(2)),
            new(categoryName, "ab", new FakeInput.Location(2), new FakeInput.Location(4))
        };
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IgnoresNonMatchingSubstrings()
    {
        var categoryName = "cat";

        // this DFA matches `ab`
        var transitions = new Dictionary<(int, char), int>
            {
                { (0, 'a'), 1 },
                { (1, 'b'), 2 }
            };
        var dfa = new FakeDfa(
            transitions,
            0,
            new HashSet<int> { 2 }
        );

        // the space isn't matched by the DFA
        var input = new FakeInput("ab ab");

        var categoryDfas = new Dictionary<string, IDfa<int, char>> { { categoryName, dfa } };

        var lexer = new Lexer<string, int>(categoryDfas);

        var result = lexer.Process(input, new NoOpDiagnostics());
        var expected = new List<Token<string>> {
            new(categoryName, "ab", input.Start, new FakeInput.Location(2)),
            new(categoryName, "ab", new FakeInput.Location(3), new FakeInput.Location(5))
        };
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MatchesOnlyTheLongerToken()
    {
        var category1 = 0;
        var category2 = 1;

        // this DFA matches `(ab)*`
        var transitions1 = new Dictionary<(int, char), int>
            {
                { (0, 'a'), 1 },
                { (1, 'b'), 0 }
            };

        var dfa1 = new FakeDfa(
            transitions1,
            0,
            new HashSet<int> { 0 }
        );

        // this DFA matches `ab`
        var transitions2 = new Dictionary<(int, char), int>
            {
                { (0, 'a'), 1 },
                { (1, 'b'), 2 }
            };

        var dfa2 = new FakeDfa(
            transitions2,
            0,
            new HashSet<int> { 2 }
        );

        var input = new FakeInput("ababab");

        var categoryDfas = new Dictionary<int, IDfa<int, char>> {
            { category1, dfa1 },
            { category2, dfa2 }
        };

        var lexer = new Lexer<int, int>(categoryDfas);

        var result = lexer.Process(input, new NoOpDiagnostics());
        Assert.Single(result, new Token<int>(category1, "ababab", input.Start, new FakeInput.Location(6)));
    }

    [Fact]
    public void MatchesAllCategories()
    {
        var category1 = 0;
        var category2 = 1;

        // this DFA matches `(ab)*`
        var transitions1 = new Dictionary<(int, char), int>
            {
                { (0, 'a'), 1 },
                { (1, 'b'), 0 }
            };

        var dfa1 = new FakeDfa(
            transitions1,
            0,
            new HashSet<int> { 0 }
        );

        // this DFA matches `(a|b)*`
        var transitions2 = new Dictionary<(int, char), int>
            {
                { (0, 'a'), 0 },
                { (0, 'b'), 0 }
            };

        var dfa2 = new FakeDfa(
            transitions2,
            0,
            new HashSet<int> { 0 }
        );

        var input = new FakeInput("ababab");

        var categoryDfas = new Dictionary<int, IDfa<int, char>> {
            { category1, dfa1 },
            { category2, dfa2 }
        };

        var lexer = new Lexer<int, int>(categoryDfas);

        // compare sets, since the order the categories are yielded in
        // is unspecified
        var result = lexer.Process(input, new NoOpDiagnostics()).ToHashSet();
        var expected = new HashSet<Token<int>> {
            new(category1, "ababab", input.Start, new FakeInput.Location(6)),
            new(category2, "ababab", input.Start, new FakeInput.Location(6))
        };
        Assert.Equal(expected, result);
    }
}
