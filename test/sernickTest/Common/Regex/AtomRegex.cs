namespace sernickTest.Common.Regex;

using Regex = sernick.Common.Regex.Regex<char>;

public class AtomRegex
{
    [Fact]
    public void When_ContainsEpsilon_Then_AlwaysReturnFalse()
    {
        var regex = Regex.Atom('a');

        Assert.False(regex.ContainsEpsilon());
    }

    [Fact]
    public void When_DerivativeByTheSameCharacter_Then_ReturnEpsilon()
    {
        var regex = Regex.Atom('a');

        Assert.True(regex.Derivative('a').Equals(Regex.Epsilon));
    }

    [Fact]
    public void When_DerivativeByOtherCharacter_Then_ReturnEmpty()
    {
        var regex = Regex.Atom('a');

        Assert.True(regex.Derivative('b').Equals(Regex.Empty));
    }

    [Fact]
    public void When_Reverse_Then_ComputeCorrectly_Case1()
    {
        var regex = Regex.Atom('a');

        Assert.Equal(regex, regex.Reverse());
    }

    [Fact]
    public void TestEqualsAndHashCode()
    {
        var regexA = Regex.Atom('a');
        var regexA2 = Regex.Atom('a');
        var regexB = Regex.Atom('b');
        var regexAB = Regex.Concat(regexA2, regexB);

        Assert.True(regexA.Equals(regexA2));
        Assert.False(regexA.Equals(regexB));
        Assert.False(regexA.Equals(regexAB));

        Assert.True(regexA.GetHashCode() == regexA2.GetHashCode());
        Assert.False(regexA.GetHashCode() == regexB.GetHashCode());
        Assert.False(regexA.GetHashCode() == regexAB.GetHashCode());
    }
}
