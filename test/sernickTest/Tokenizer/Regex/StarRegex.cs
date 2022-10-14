namespace sernickTest.Tokenizer.Regex;

using sernick.Tokenizer.Regex;

public class StarRegex
{
    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    /*
     * Rule 1: X^^ == X^
     */
    public void When_CreateStarRegex_Then_NormalizesCorrectly_Rule1()
    {
        var regex = Regex.Star(Regex.Star(Regex.Union(
            Regex.Atom('a'), Regex.Atom('b')
        )));
        var normalizedRegex = Regex.Star(Regex.Union(
            Regex.Atom('a'), Regex.Atom('b')
        ));

        Assert.True(regex.Equals(normalizedRegex));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    /*
     * Rule 2: \eps^ == \eps
     */
    public void When_CreateStarRegex_Then_NormalizesCorrectly_Rule2()
    {
        var regex = Regex.Star(Regex.Epsilon);

        Assert.True(regex.Equals(Regex.Epsilon));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    /*
     * Rule 3: \empty^ == \eps
     */
    public void When_CreateStarRegex_Then_NormalizesCorrectly_Rule3()
    {
        var regex = Regex.Star(Regex.Empty);

        Assert.True(regex.Equals(Regex.Epsilon));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_CreateNestedStarRegex_Then_NormalizesCorrectly()
    {
        var regex = Regex.Star(Regex.Star(Regex.Star(Regex.Atom('a'))));
        var normalizedRegex = Regex.Atom('a');

        Assert.True(regex.Equals(normalizedRegex));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_ContainsEpsilon_Then_AlwaysReturnTrue()
    {
        var regex = Regex.Star(Regex.Empty);

        Assert.True(regex.ContainsEpsilon());
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_Derivative_Then_ComputeCorrectly_Case1()
    {
        var regex = Regex.Star(Regex.Atom('a'));

        Assert.True(regex.Derivative('a').Equals(regex));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_Derivative_Then_ComputeCorrectly_Case2()
    {
        var regex = Regex.Star(Regex.Atom('a'));

        Assert.True(regex.Derivative('b').Equals(Regex.Empty));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_Derivative_Then_ComputeCorrectly_Case3()
    {
        var regex = Regex.Star(
            Regex.Concat(
                Regex.Atom('a'),
                Regex.Atom('b')
            )
        );

        var result = Regex.Union(Regex.Atom('b'), regex);

        Assert.True(regex.Derivative('a').Equals(result));
    }

    [Fact]
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
        Assert.False(regexAstar.GetHashCode() == regexA.GetHashCode());
    }
}
