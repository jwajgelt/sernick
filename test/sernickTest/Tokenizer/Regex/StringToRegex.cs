namespace sernickTest.Tokenizer.Regex;

using sernick.Tokenizer.Regex;
using Regex = sernick.Common.Regex.Regex<char>;

public class StringToRegex
{
    [Fact]
    public void TestAtom()
    {
        var actualRegex = "a".ToRegex();
        var expectedRegex = Regex.Atom('a');

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestUnion()
    {
        var actualRegex = "a|b".ToRegex();
        var expectedRegex = Regex.Union(Regex.Atom('a'), Regex.Atom('b'));

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestConcat()
    {
        var actualRegex = "ab".ToRegex();
        var expectedRegex = Regex.Concat(Regex.Atom('a'), Regex.Atom('b'));

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestStar()
    {
        var actualRegex = "a*".ToRegex();
        var expectedRegex = Regex.Star(Regex.Atom('a'));

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestPlus()
    {
        var actualRegex = "a+".ToRegex();
        var expectedRegex = Regex.Concat(Regex.Atom('a'), Regex.Star(Regex.Atom('a')));

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestEscape()
    {
        var actualRegex = @"\[\+\*\.".ToRegex();
        var expectedRegex = Regex.Concat(Regex.Atom('['), Regex.Atom('+'), Regex.Atom('*'), Regex.Atom('.'));

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestEscapeBackslash()
    {
        var actualRegex = @"\\\[".ToRegex();
        var expectedRegex = Regex.Concat(Regex.Atom('\\'), Regex.Atom('['));

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestCharacterClass()
    {
        var actualRegex = "[[:digit:]]".ToRegex();
        var expectedRegex = Regex.Union("0123456789".Select(Regex.Atom));

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestMultipleCharacterClasses()
    {
        var actualRegex = "[[:digit:]][[:lower:]]".ToRegex();
        var expectedRegex = Regex.Concat(Regex.Union("0123456789".Select(Regex.Atom)),
            Regex.Union("qwertyuiopasdfghjkklzxcvbnm".Select(Regex.Atom)));

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestDot()
    {
        var actualRegex = ".".ToRegex();

        var children = new List<Regex>();
        for (var c = ' '; c <= '~'; c++)
        {
            children.Add(Regex.Atom(c));
        }

        var expectedRegex = Regex.Union(children);

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestComplexRegex()
    {
        var actualRegex = "((a|b|c)|d*)+((a*b)c)*".ToRegex();
        var left = Regex.Union(Regex.Union(Regex.Atom('a'), Regex.Atom('b'), Regex.Atom('c')), Regex.Star(Regex.Atom('d')));
        var right = Regex.Concat(Regex.Concat(Regex.Star(Regex.Atom('a')), Regex.Atom('b')), Regex.Atom('c'));
        var expectedRegex = Regex.Concat(Regex.Concat(left, Regex.Star(left)), Regex.Star(right));

        Assert.Equal(expectedRegex, actualRegex);
    }

    [Fact]
    public void TestExceptions()
    {
        var regexString1 = "[[]";
        var regexString2 = "[[:x:]]";
        var regexString3 = "*";
        var regexString4 = "(abc|)|";

        Assert.ThrowsAny<Exception>(() => regexString1.ToRegex());
        Assert.ThrowsAny<Exception>(() => regexString2.ToRegex());
        Assert.ThrowsAny<Exception>(() => regexString3.ToRegex());
        Assert.ThrowsAny<Exception>(() => regexString4.ToRegex());
    }
}
