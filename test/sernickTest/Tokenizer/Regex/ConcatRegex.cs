namespace sernickTest.Tokenizer.Regex;

using sernick.Tokenizer.Regex;
using StarRegex_ = sernick.Tokenizer.Regex.StarRegex;
using UnionRegex_ = sernick.Tokenizer.Regex.UnionRegex;

public class ConcatRegex
{
    [Fact]
    public void When_CreateEmptyConcatRegex_Then_ReturnsEpsilonRegex()
    {
        var regex = Regex.Concat(new List<Regex>());

        // Assert.True(regex.Equals(Regex.Epsilon));

        // NOTE: naive checks follow
        Assert.IsType<StarRegex_>(regex);
        var starRegex = regex as StarRegex_;
        Assert.IsType<UnionRegex_>(starRegex.Child);
        var unionRegex = starRegex.Child as UnionRegex_;
        Assert.Empty(unionRegex.Children);
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_CreateSingletonConcatRegex_Then_ReturnsItsContents()
    {
        var regex = Regex.Concat(Regex.Atom('a'));

        Assert.True(regex.Equals(Regex.Atom('a')));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    /*
     * Rule 1: \empty * X == X * \empty == \empty
     */
    public void When_CreateConcatRegex_Then_NormalizesCorrectly_Rule1()
    {
        var regex = Regex.Concat(
            Regex.Concat(
                Regex.Atom('a'),
                Regex.Star(Regex.Atom('b'))
            ),
            Regex.Empty,
            Regex.Atom('c')
        );

        Assert.True(regex.Equals(Regex.Empty));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    /*
     * Rule 2: \eps * X == X * \eps == X
     */
    public void When_CreateConcatRegex_Then_NormalizesCorrectly_Rule2()
    {
        var regex = Regex.Concat(
            Regex.Concat(
                Regex.Atom('a'),
                Regex.Star(Regex.Atom('b'))
            ),
            Regex.Epsilon,
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

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    /*
     * Rule 3: (X * Y) * Z == X * (Y * Z)
     */
    public void When_CreateConcatRegex_Then_NormalizesCorrectly_Rule3()
    {
        var regex1 = Regex.Concat(
            Regex.Concat(
                Regex.Atom('a'),
                Regex.Atom('b')
            ),
            Regex.Atom('c')
        );
        var regex2 = Regex.Concat(
            Regex.Atom('a'),
            Regex.Concat(
                Regex.Atom('b'),
                Regex.Atom('c')
            )
        );

        Assert.True(regex1.Equals(regex2));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_CreateNestedConcatRegex_Then_NormalizesCorrectly()
    {
        var regex = Regex.Concat(
            Regex.Concat(
                Regex.Concat(
                    Regex.Atom('a')
                )
            )
        );
        var normalizedRegex = Regex.Atom('a');

        Assert.True(regex.Equals(normalizedRegex));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_AllRegexesContainEpsilon_Then_ReturnTrue()
    {
        var regex = Regex.Concat(
            Regex.Star(Regex.Atom('a')),
            Regex.Star(Regex.Atom('b')),
            Regex.Star(Regex.Atom('c'))
        );

        Assert.True(regex.ContainsEpsilon());
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_OneRegexContainsEpsilon_Then_ReturnFalse()
    {
        var regex = Regex.Concat(
            Regex.Star(Regex.Atom('a')),
            Regex.Atom('b'),
            Regex.Atom('c')
        );

        Assert.False(regex.ContainsEpsilon());
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_NoneRegexContainsEpsilon_Then_ReturnFalse()
    {
        var regex = Regex.Concat(
            Regex.Atom('a'),
            Regex.Atom('b'),
            Regex.Atom('c')
        );

        Assert.False(regex.ContainsEpsilon());
    }

    /*
     * Rule 1: When X hasn't epsilon than
     * (XY)' = X'Y
     */
    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_Derivative_Then_ComputeCorrectly_Rule1()
    {
        var regex = Regex.Concat(
            Regex.Atom('a'),
            Regex.Atom('b')
        );

        Assert.True(regex.Derivative('a').Equals(Regex.Atom('b')));
    }

    /*
     * Rule 2: When X has epsilon than
     * (XY)' = X'Y \cup Y'
     */
    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_Derivative_Then_ComputeCorrectly_Rule2()
    {
        var regex = Regex.Concat(new List<Regex>
        {
            Regex.Star(Regex.Atom('a')),
            Regex.Atom('a'),
            Regex.Atom('b')
        });

        var result = Regex.Union(regex, Regex.Atom('b'));

        Assert.True(regex.Derivative('a').Equals(result));
    }
}
