namespace sernickTest.Grammar.Syntax;

using Common.Dfa.Helpers;
using Dfa.Helpers;
using sernick.Grammar.Syntax;
using sernick.Tokenizer.Regex;

public class GrammarConversionTest
{
    [Fact]
    public void GrammarConversion_contains_all_left_symbols()
    {
        var convertedGrammar = new Grammar<char>('S', new[]
        {
            new Production<char>('S', "AB".ToRegex()),
            new Production<char>('A', "a".ToRegex()),
            new Production<char>('B', "b".ToRegex())
        }).ToDfaGrammar();

        Assert.Equal(new[] { 'A', 'B', 'S' }, convertedGrammar.GetLeftSymbols());
    }

    [Fact]
    public void GrammarConversion_combines_productions_with_the_same_left_symbol()
    {
        var convertedGrammar = new Grammar<char>('S', new[]
        {
            new Production<char>('S', "AB".ToRegex()),
            new Production<char>('S', "BA".ToRegex()),
            new Production<char>('A', "CD".ToRegex()),
        }).ToDfaGrammar();

        Assert.Equal(new[] { 'A', 'S' }, convertedGrammar.GetLeftSymbols());
        Assert.True(convertedGrammar.Productions['S'].AcceptsText("AB"));
        Assert.True(convertedGrammar.Productions['S'].AcceptsText("BA"));
        Assert.False(convertedGrammar.Productions['S'].AcceptsText("CD"));
    }

    [Fact]
    public void Converted_Grammar_recognises_accepting_production()
    {
        var productions = new[]
        {
            new Production<char>('S', "abc".ToRegex()),
            new Production<char>('S', "pqr".ToRegex()),
            new Production<char>('S', "xyz".ToRegex()),
        };
        var convertedGrammar = new Grammar<char>('S', productions).ToDfaGrammar();
        var productionsAcceptingAbc = new[] { productions[0] };

        Assert.Equal(productionsAcceptingAbc, convertedGrammar.AcceptingProductions("abc"));
    }

    [Fact]
    public void ConvertedGrammar_recognises_all_accepting_productions()
    {
        var productions = new[]
        {
            new Production<char>('A', "a*".ToRegex()),
            new Production<char>('B', "b*".ToRegex()),
            new Production<char>('C', "c*".ToRegex()),
        };
        var convertedGrammar = new Grammar<char>('S', productions).ToDfaGrammar();

        // All productions accept epsilon 
        Assert.Equal(productions, convertedGrammar.AcceptingProductions(""));
    }

    [Fact]
    public void ConvertedGrammar_recognises_all_accepting_productions_with_same_left_symbol()
    {
        var productions = new[]
        {
            new Production<char>('S', "a*".ToRegex()),
            new Production<char>('S', "b*".ToRegex()),
            new Production<char>('S', "c*".ToRegex()),
        };
        var convertedGrammar = new Grammar<char>('S', productions).ToDfaGrammar();

        // All productions accept epsilon 
        Assert.Equal(productions, convertedGrammar.AcceptingProductions(""));
    }

    [Fact]
    public void ConvertedGrammar_has_correct_TransitionsFrom_in_combined_productions()
    {
        var convertedGrammar = new Grammar<char>('S', new[]
        {
            new Production<char>('S', "a*".ToRegex()),
            new Production<char>('S', "b*".ToRegex()),
            new Production<char>('S', "c*".ToRegex()),
        }).ToDfaGrammar();

        var sDfa = convertedGrammar.Productions['S'];
        var transitionAtoms = sDfa.GetTransitionsFrom(sDfa.Start)
            .Select(edge => edge.Atom)
            .OrderBy(atom => atom);

        Assert.Equal(new[] { 'a', 'b', 'c' }, transitionAtoms);
    }

    [Fact]
    public void ConvertedGrammar_has_correct_TransitionsTo_in_combined_productions()
    {
        var convertedGrammar = new Grammar<char>('S', new[]
        {
            new Production<char>('S', "a*".ToRegex()),
            new Production<char>('S', "aa*".ToRegex()),
            new Production<char>('S', "aaa*".ToRegex()),
        }).ToDfaGrammar();

        var sDfa = convertedGrammar.Productions['S'];
        var movedState = sDfa.Transition(sDfa.Start, "a");
        var transitionAtoms = sDfa.GetTransitionsTo(movedState)
            .Select(edge => edge.Atom)
            .OrderBy(atom => atom);

        Assert.Equal(new[] { 'a' }, transitionAtoms);
    }
}
