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

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_CreateSingletonUnionRegex_Then_ReturnsItsContents()
    {
        var regex = Regex.Union(new List<Regex> { Regex.Atom('a') });

        Assert.True(regex.Equals(Regex.Atom('a')));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    /*
     * Rule 1: X \cup X == X
     */
    public void When_CreateUnionRegex_Then_NormalizesCorrectly_Rule1()
    {
        var regex = Regex.Union(new List<Regex>
        {
            Regex.Concat(new List<Regex>
            {
                Regex.Atom('a'),
                Regex.Star(Regex.Atom('b'))
            }),
            Regex.Concat(new List<Regex>
            {
                Regex.Atom('a'),
                Regex.Star(Regex.Atom('b'))
            })
        });
        var normalizedRegex = Regex.Concat(new List<Regex>
        {
            Regex.Atom('a'),
            Regex.Star(Regex.Atom('b'))
        });

        Assert.True(regex.Equals(normalizedRegex));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    /*
     * Rule 2: \empty \cup X == X
     */
    public void When_CreateUnionRegex_Then_NormalizesCorrectly_Rule2()
    {
        var regex = Regex.Union(new List<Regex>
        {
            Regex.Concat(new List<Regex>
            {
                Regex.Atom('a'),
                Regex.Star(Regex.Atom('b'))
            }),
            Regex.Empty,
            Regex.Atom('c')
        });
        var normalizedRegex = Regex.Union(new List<Regex>
        {
            Regex.Concat(new List<Regex>
            {
                Regex.Atom('a'),
                Regex.Star(Regex.Atom('b'))
            }),
            Regex.Atom('c')
        });

        Assert.True(regex.Equals(normalizedRegex));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    /*
     * Rule 3: (X \cup Y) \cup Z == X \cup (Y \cup Z)
     */
    public void When_CreateUnionRegex_Then_NormalizesCorrectly_Rule3()
    {
        var regex1 = Regex.Union(new List<Regex>
        {
            Regex.Union(new List<Regex>
            {
                Regex.Atom('a'),
                Regex.Atom('b')
            }),
            Regex.Atom('c')
        });
        var regex2 = Regex.Union(new List<Regex>
        {
            Regex.Atom('a'),
            Regex.Union(new List<Regex>
            {
                Regex.Atom('b'),
                Regex.Atom('c')
            }),
        });

        Assert.True(regex1.Equals(regex2));
    }

    [Fact(Skip = "No Regex.Equals(Regex) implementation at the moment")]
    public void When_CreateNestedUnionRegex_Then_NormalizesCorrectly()
    {
        var regex = Regex.Union(new List<Regex>
        {
            Regex.Union(new List<Regex>
            {
                Regex.Union(new List<Regex>
                {
                    Regex.Atom('a')
                })
            })
        });
        var normalizedRegex = Regex.Atom('a');

        Assert.True(regex.Equals(normalizedRegex));
    }
}
