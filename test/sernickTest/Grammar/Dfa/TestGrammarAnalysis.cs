namespace sernickTest.Grammar.Dfa;
using sernick.Common.Dfa;
using Regex = sernick.Common.Regex.Regex<char>;
using RegexDfa = sernick.Common.Dfa.RegexDfa<char>;
using GrammarAnalysis = sernick.Grammar.Dfa.GrammarAnalysis;

public class TestGrammarAnalysis
{
    private IDfaWithConfig<Regex, char> SingleAcceptingStateNoTransitions()
    {
        var emptyRegex = Regex.Epsilon;
        return RegexDfa.FromRegex(emptyRegex);
    }

    [Fact]
    public void Correctly_handles_multiple_empty_automatas()
    {
        List<char> Alphabet = new List<char>() { 'A', 'B', 'C' };
        IReadOnlyDictionary<char, IDfaWithConfig<Regex, char>> Productions = new Dictionary<char, IDfaWithConfig<Regex, char>>()
        {
            ['A'] = SingleAcceptingStateNoTransitions(),
            ['B'] = SingleAcceptingStateNoTransitions(),
            ['C'] = SingleAcceptingStateNoTransitions()
        };

        var testGrammar = new sernick.Grammar.Dfa.DfaGrammar<char, Regex>('X', Productions);

        Console.WriteLine(string.Join("\t", Productions['A'].AcceptingStates));

        var Nullable = GrammarAnalysis.Nullable(testGrammar).ToList();
        Alphabet.Sort();
        Nullable.Sort();
        Console.WriteLine(string.Join("\t", Nullable));
        Console.WriteLine(string.Join("\t", Alphabet));
        Assert.True(Equals(Alphabet, Nullable));
    }

}