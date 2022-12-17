namespace sernickTest.Utility;

using sernick.Utility;

public class MultisetTest
{
    [Fact]
    public void NeverAddedItemsReturnQuantityZero()
    {
        var set = new Multiset<string>();

        var amount = set.Get("a");

        Assert.Equal(0, amount);
    }

    [Fact]
    public void CountsItemsCorrectly()
    {
        var set = new Multiset<string>();
        
        set.Add("a");
        set.Add("b");
        set.Add("a");
        set.Add("b");
        set.Add("a");
        
        Assert.Equal(3, set.Get("a"));
        Assert.Equal(2, set.Get("b"));
    }
}
