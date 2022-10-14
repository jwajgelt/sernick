namespace sernickTest.Tokenizer.Regex;

using sernick.Tokenizer.Regex;

public class AtomRegex
{
    [Fact]
    public void When_ContainsEpsilon_Then_AlwaysReturnFalse()
    {
        var regex = Regex.Atom('a');

        Assert.False(regex.ContainsEpsilon());
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_DerivativeByTheSameCharacter_Then_ReturnEpsilon()
    {
        var regex = Regex.Atom('a');

        Assert.True(regex.Derivative('a').Equals(Regex.Epsilon));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_DerivativeByOtherCharacter_Then_ReturnEmpty()
    {
        var regex = Regex.Atom('a');

        Assert.True(regex.Derivative('b').Equals(Regex.Empty));
    }
}
