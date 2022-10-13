namespace sernickTest.Tokenizer.Regex;

using sernick.Tokenizer.Regex;

public class StarRegex
{
    [Fact(Skip = "No Regex static methods implementation")]
    public void TestEqualsAndHashCode()
    {
        var regexA = Regex.Atom('a');
        var regexA2 = Regex.Atom('a');
        var regexB = Regex.Atom('b');
        var regexAstar = Regex.Star(regexA);
        var regexA2star = Regex.Star(regexA2);
        var regexBstar = Regex.Star(regexB);

        Assert.True(regexAstar.Equals(regexA2star));
        Assert.False(regexAstar.Equals(regexBstar));
        Assert.False(regexAstar.Equals(regexA));

        Assert.True(regexAstar.GetHashCode() == regexA2star.GetHashCode());
        Assert.False(regexAstar.GetHashCode() == regexBstar.GetHashCode());
    }
}
