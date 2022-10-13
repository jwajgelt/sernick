namespace sernickTest.Tokenizer.Regex;

using sernick.Tokenizer.Regex;

public class UnionRegex
{
    [Fact(Skip = "No Regex static methods implementation")]
    public void TestEqualsAndHashCode()
    {
        var regexA = Regex.Atom('a');
        var regexA2 = Regex.Atom('a');
        var regexB = Regex.Atom('b');
        var regexC = Regex.Atom('c');
        var regexAB = Regex.Union(new Regex[] { regexA, regexB });
        var regexAB2 = Regex.Union(new Regex[] { regexA2, regexB });
        var regexBA = Regex.Union(new Regex[] { regexB, regexA });
        var regexABC = Regex.Union(new Regex[] { regexA, regexB, regexC });

        Assert.True(regexAB.Equals(regexAB2));
        Assert.True(regexAB.Equals(regexBA));
        Assert.False(regexAB.Equals(regexABC));

        Assert.True(regexAB.GetHashCode() == regexAB2.GetHashCode());
        Assert.True(regexAB.GetHashCode() == regexBA.GetHashCode());
        Assert.False(regexAB.GetHashCode() == regexABC.GetHashCode());
    }
}
