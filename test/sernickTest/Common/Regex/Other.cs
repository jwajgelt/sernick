namespace sernickTest.Common.Regex;

using Regex = sernick.Common.Regex.Regex<char>;

public class Other
{
    [Fact]
    public void When_Reverse_Then_ComputeCorrectly_Case1()
    {
        var regex = Regex.Epsilon;

        Assert.Equal(regex, regex.Reverse());
    }

    [Fact]
    public void When_Reverse_Then_ComputeCorrectly_Case2()
    {
        var regex = Regex.Empty;

        Assert.Equal(regex, regex.Reverse());
    }
}
