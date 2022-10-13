using sernick.Tokenizer;
using sernick.Tokenizer.Lexer;

namespace sernickTest.Tokenizer.Lexer;

public class LexerTest
{
    [Fact]
    public void ReturnsMatching()
    {
        const string CATEGORY_NAME = "cat";

        var transitions = new Dictionary<(int, char), int>()
            {
                { (0, 'a'), 0}
            };
        var dfa = new DfaMock(
            transitions,
            0,
            new HashSet<int>() { 0 }
        );

        var input = new InputMock("a");

        var categoryDfas = new Dictionary<string, sernick.Tokenizer.Dfa.IDfa<int>>();
        categoryDfas.Add(CATEGORY_NAME, dfa);

        var lexer = new Lexer<string, int>(categoryDfas);

        var result = lexer.Process(input).ToList();
        Assert.Contains(new Token<string>(CATEGORY_NAME, "a", input.Start, input.End), result);
        Assert.Single(result);
    }

    [Fact]
    public void DoesNotReturnNonMatching()
    {
        const string CATEGORY_NAME = "cat";

        var transitions = new Dictionary<(int, char), int>()
            {
                { (0, 'a'), 0}
            };
        var dfa = new DfaMock(
            transitions,
            0,
            new HashSet<int>() { 0 }
        );

        var input = new InputMock("b");

        var categoryDfas = new Dictionary<string, sernick.Tokenizer.Dfa.IDfa<int>>();
        categoryDfas.Add(CATEGORY_NAME, dfa);

        var lexer = new Lexer<string, int>(categoryDfas);

        var result = lexer.Process(input).ToList();
        Assert.Empty(result);
    }

    [Fact]
    public void MatchesTheLongestPossibleToken()
    {
        const string CATEGORY_NAME = "cat";
        var transitions = new Dictionary<(int, char), int>()
            {
                { (0, 'a'), 0 }
            };
        var dfa = new DfaMock(
            transitions,
            0,
            new HashSet<int>() { 0 }
        );

        var input = new InputMock("aaaab");

        var categoryDfas = new Dictionary<string, sernick.Tokenizer.Dfa.IDfa<int>>();
        categoryDfas.Add(CATEGORY_NAME, dfa);

        var lexer = new Lexer<string, int>(categoryDfas);

        var result = lexer.Process(input).ToList();
        Assert.Contains(new Token<string>(CATEGORY_NAME, "aaaa", input.Start, new InputMock.Location(4)), result);
        Assert.Single(result);
    }

    [Fact]
    public void MatchesMultipleTokens()
    {
        const string CATEGORY_NAME = "cat";

        // this DFA matches `ab`
        var transitions = new Dictionary<(int, char), int>()
            {
                { (0, 'a'), 1 },
                { (1, 'b'), 2 }
            };
        var dfa = new DfaMock(
            transitions,
            0,
            new HashSet<int>() { 2 }
        );

        var input = new InputMock("abab");

        var categoryDfas = new Dictionary<string, sernick.Tokenizer.Dfa.IDfa<int>>();
        categoryDfas.Add(CATEGORY_NAME, dfa);

        var lexer = new Lexer<string, int>(categoryDfas);

        var result = lexer.Process(input).ToList();
        Assert.Contains(new Token<string>(CATEGORY_NAME, "ab", input.Start, new InputMock.Location(2)), result);
        Assert.Contains(new Token<string>(CATEGORY_NAME, "ab", new InputMock.Location(2), new InputMock.Location(4)), result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void IgnoresNonMatchingSubstrings()
    {
        const string CATEGORY_NAME = "cat";

        // this DFA matches `ab`
        var transitions = new Dictionary<(int, char), int>()
            {
                { (0, 'a'), 1 },
                { (1, 'b'), 2 }
            };
        var dfa = new DfaMock(
            transitions,
            0,
            new HashSet<int>() { 2 }
        );

        // the space isn't matched by the DFA
        var input = new InputMock("ab ab");

        var categoryDfas = new Dictionary<string, sernick.Tokenizer.Dfa.IDfa<int>>();
        categoryDfas.Add(CATEGORY_NAME, dfa);

        var lexer = new Lexer<string, int>(categoryDfas);

        var result = lexer.Process(input).ToList();
        Assert.Contains(new Token<string>(CATEGORY_NAME, "ab", input.Start, new InputMock.Location(2)), result);
        Assert.Contains(new Token<string>(CATEGORY_NAME, "ab", new InputMock.Location(3), new InputMock.Location(5)), result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void MatchesOnlyTheLongerToken()
    {
        const int CATEGORY_1 = 0;
        const int CATEGORY_2 = 1;

        // this DFA matches `(ab)*`
        var transitions1 = new Dictionary<(int, char), int>()
            {
                { (0, 'a'), 1 },
                { (1, 'b'), 0 }
            };

        var dfa1 = new DfaMock(
            transitions1,
            0,
            new HashSet<int>() { 0 }
        );

        // this DFA matches `ab`
        var transitions2 = new Dictionary<(int, char), int>()
            {
                { (0, 'a'), 1 },
                { (1, 'b'), 2 }
            };

        var dfa2 = new DfaMock(
            transitions2,
            0,
            new HashSet<int>() { 2 }
        );

        var input = new InputMock("ababab");

        var categoryDfas = new Dictionary<int, sernick.Tokenizer.Dfa.IDfa<int>>();
        categoryDfas.Add(CATEGORY_1, dfa1);
        categoryDfas.Add(CATEGORY_2, dfa2);

        var lexer = new Lexer<int, int>(categoryDfas);

        var result = lexer.Process(input).ToList();
        Assert.Contains(new Token<int>(CATEGORY_1, "ababab", input.Start, new InputMock.Location(6)), result);
        Assert.Single(result);
    }

    [Fact]
    public void MatchesAllCategories()
    {
        const int CATEGORY_1 = 0;
        const int CATEGORY_2 = 1;

        // this DFA matches `(ab)*`
        var transitions1 = new Dictionary<(int, char), int>()
            {
                { (0, 'a'), 1 },
                { (1, 'b'), 0 }
            };

        var dfa1 = new DfaMock(
            transitions1,
            0,
            new HashSet<int>() { 0 }
        );

        // this DFA matches `(a|b)*`
        var transitions2 = new Dictionary<(int, char), int>()
            {
                { (0, 'a'), 0 },
                { (0, 'b'), 0 }
            };

        var dfa2 = new DfaMock(
            transitions2,
            0,
            new HashSet<int>() { 0 }
        );

        var input = new InputMock("ababab");

        var categoryDfas = new Dictionary<int, sernick.Tokenizer.Dfa.IDfa<int>>();
        categoryDfas.Add(CATEGORY_1, dfa1);
        categoryDfas.Add(CATEGORY_2, dfa2);

        var lexer = new Lexer<int, int>(categoryDfas);

        var result = lexer.Process(input).ToList();
        Assert.Contains(new Token<int>(CATEGORY_1, "ababab", input.Start, new InputMock.Location(6)), result);
        Assert.Contains(new Token<int>(CATEGORY_2, "ababab", input.Start, new InputMock.Location(6)), result);
        Assert.Equal(2, result.Count);
    }
}
