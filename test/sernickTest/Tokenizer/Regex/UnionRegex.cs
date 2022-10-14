namespace sernickTest.Tokenizer.Regex;

using sernick.Tokenizer.Regex;
using UnionRegex_ = sernick.Tokenizer.Regex.UnionRegex;

public class UnionRegex
{
    [Fact]
    public void When_CreateEmptyUnionRegex_Then_ReturnsEmptyRegex()
    {
        var regex = Regex.Union(new List<Regex>());

        // Assert.True(regex.Equals(Regex.Empty));

        // NOTE: naive checks follow
        Assert.IsType<UnionRegex_>(regex);
        var unionRegex = regex as UnionRegex_;
        Assert.Empty(unionRegex.Children);
    }

    [Fact]
    public void When_CreateSingletonUnionRegex_Then_ReturnsItsContents()
    {
        var regex = Regex.Union(Regex.Atom('a'));

        Assert.True(regex.Equals(Regex.Atom('a')));
    }

    [Fact]
    /*
     * Rule 1: X \cup X == X
     */
    public void When_CreateUnionRegex_Then_NormalizesCorrectly_Rule1()
    {
        var regex = Regex.Union(
            Regex.Concat(
                Regex.Atom('a'),
                Regex.Star(Regex.Atom('b'))
            ),
            Regex.Concat(
                Regex.Atom('a'),
                Regex.Star(Regex.Atom('b'))
            )
        );
        var normalizedRegex = Regex.Concat(
            Regex.Atom('a'),
            Regex.Star(Regex.Atom('b'))
        );

        Assert.True(regex.Equals(normalizedRegex));
    }

    [Fact]
    /*
     * Rule 2: \empty \cup X == X
     */
    public void When_CreateUnionRegex_Then_NormalizesCorrectly_Rule2()
    {
        var regex = Regex.Union(
            Regex.Concat(
                Regex.Atom('a'),
                Regex.Star(Regex.Atom('b'))
            ),
            Regex.Empty,
            Regex.Atom('c')
        );
        var normalizedRegex = Regex.Union(
            Regex.Concat(
                Regex.Atom('a'),
                Regex.Star(Regex.Atom('b'))
            ),
            Regex.Atom('c')
        );

        Assert.True(regex.Equals(normalizedRegex));
    }

    [Fact]
    /*
     * Rule 3: (X \cup Y) \cup Z == X \cup (Y \cup Z)
     */
    public void When_CreateUnionRegex_Then_NormalizesCorrectly_Rule3()
    {
        var regex1 = Regex.Union(
            Regex.Union(
                Regex.Atom('a'),
                Regex.Atom('b')
            ),
            Regex.Atom('c')
        );
        var regex2 = Regex.Union(
            Regex.Atom('a'),
            Regex.Union(
                Regex.Atom('b'),
                Regex.Atom('c')
            )
        );

        Assert.True(regex1.Equals(regex2));
    }

    [Fact]
    public void When_CreateNestedUnionRegex_Then_NormalizesCorrectly()
    {
        var regex = Regex.Union(
            Regex.Union(
                Regex.Union(
                    Regex.Atom('a')
                )
            )
        );
        var normalizedRegex = Regex.Atom('a');

        Assert.True(regex.Equals(normalizedRegex));
    }

    [Fact]
    public void When_AllRegexesContainEpsilon_Then_ReturnTrue()
    {
        var regex = Regex.Union(
            Regex.Epsilon,
            Regex.Star(Regex.Atom('a')),
            Regex.Star(Regex.Atom('b'))
        );

        Assert.True(regex.ContainsEpsilon());
    }

    [Fact]
    public void When_OneRegexContainsEpsilon_Then_ReturnTrue()
    {
        var regex = Regex.Union(
            Regex.Epsilon,
            Regex.Atom('a'),
            Regex.Atom('b')
        );

        Assert.True(regex.ContainsEpsilon());
    }

    [Fact]
    public void When_NoneRegexContainsEpsilon_Then_ReturnFalse()
    {
        var regex = Regex.Union(
            Regex.Atom('a'),
            Regex.Atom('b'),
            Regex.Atom('c')
            );

        Assert.False(regex.ContainsEpsilon());
    }

    [Fact]
    public void When_Derivative_Then_ComputeCorrectly_Case1()
    {
        var regex = Regex.Union(
            Regex.Atom('a'),
            Regex.Atom('b')
        );

        Assert.True(regex.Derivative('a').Equals(Regex.Epsilon));
    }

    [Fact]
    public void When_Derivative_Then_ComputeCorrectly_Case2()
    {
        var regex = Regex.Union(
            Regex.Atom('a'),
            Regex.Atom('b')
        );

        Assert.True(regex.Derivative('c').Equals(Regex.Empty));
    }

    [Fact]
    public void TestEqualsAndHashCode()
    {
        var regexA = Regex.Atom('a');
        var regexA2 = Regex.Atom('a');
        var regexB = Regex.Atom('b');
        var regexC = Regex.Atom('c');
        var regexAB = Regex.Union(regexA, regexB);
        var regexAB2 = Regex.Union(regexA2, regexB);
        var regexBA = Regex.Union(regexB, regexA);
        var regexABC = Regex.Union(regexA, regexB, regexC);

        Assert.True(regexAB.Equals(regexAB2));
        Assert.True(regexAB.Equals(regexBA));
        Assert.False(regexAB.Equals(regexABC));

        Assert.True(regexAB.GetHashCode() == regexAB2.GetHashCode());
        Assert.True(regexAB.GetHashCode() == regexBA.GetHashCode());
        Assert.False(regexAB.GetHashCode() == regexABC.GetHashCode());
    }
}
