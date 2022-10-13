namespace sernickTest.Tokenizer.Regex;

using sernick.Tokenizer.Regex;

public class ConcatRegex
{
    [Fact(Skip = "No Regex static methods implementation")]
    public void TestEqualsAndHashCode()
    {
        var regexA = Regex.Atom('a');
        var regexA2 = Regex.Atom('a');
        var regexB = Regex.Atom('b');
        var regexAB = Regex.Concat(new Regex[] { regexA, regexB });
        var regexAB2 = Regex.Concat(new Regex[] { regexA2, regexB });
        var regexABB = Regex.Concat(new Regex[] { regexA, regexB, regexB });
        var regexBA = Regex.Concat(new Regex[] { regexB, regexA });

        Assert.True(regexAB.Equals(regexAB2));
        Assert.False(regexAB.Equals(regexABB));
        Assert.False(regexAB.Equals(regexBA));

        Assert.True(regexAB.GetHashCode() == regexAB2.GetHashCode());
        Assert.False(regexAB.GetHashCode() == regexABB.GetHashCode());
        Assert.False(regexAB.GetHashCode() == regexBA.GetHashCode());
    }
}
