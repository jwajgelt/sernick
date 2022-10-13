namespace sernickTest.Tokenizer.Regex;

using sernick.Tokenizer.Regex;

public class AtomRegex
{
    [Fact]
    public void TestEqualsAndHashCode()
    {
        var regexA = Regex.Atom('a');
        var regexA2 = Regex.Atom('a');
        var regexB = Regex.Atom('b');
        var regexAB = Regex.Concat(new Regex[] { regexA2, regexB });

        Assert.True(regexA.Equals(regexA2));
        Assert.False(regexA.Equals(regexB));
        Assert.False(regexA.Equals(regexAB));

        Assert.True(regexA.GetHashCode() == regexA2.GetHashCode());
        Assert.False(regexA.GetHashCode() == regexB.GetHashCode());
        Assert.False(regexA.GetHashCode() == regexAB.GetHashCode());
    }
}
