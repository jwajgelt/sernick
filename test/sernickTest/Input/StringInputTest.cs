using sernick.Input;
using sernick.Input.String;

namespace sernickTest.Input;

public class StringInputTest
{
    [Fact]
    public void EmptyString()
    {
        IInput stringInput = new StringInput("");

        var currentBefore = stringInput.Current;
        var move = stringInput.MoveNext();
        var currentAfter = stringInput.Current;

        Assert.Equal(0, currentBefore);
        Assert.False(move);
        Assert.Equal(0, currentAfter);
    }

    [Fact]
    public void BasicString()
    {
        IInput stringInput = new StringInput("ab");

        var currentBefore = stringInput.Current;
        var move0 = stringInput.MoveNext();
        var current0 = stringInput.Current;
        var move1 = stringInput.MoveNext();
        var current1 = stringInput.Current;
        var move2 = stringInput.MoveNext();
        var currentAfter = stringInput.Current;

        Assert.Equal(0, currentBefore);
        Assert.True(move0);
        Assert.Equal('a', current0);
        Assert.True(move1);
        Assert.Equal('b', current1);
        Assert.False(move2);
        Assert.Equal(0, currentAfter);
    }

    [Fact]
    public void StartEqualsCurrentBeforeIterating()
    {
        IInput stringInput = new StringInput("abc");

        var start = stringInput.Start;
        var currentLocation = stringInput.CurrentLocation;

        Assert.True(start.Equals(currentLocation));
    }

    [Fact]
    public void EndEqualsCurrentAfterIterating()
    {
        IInput stringInput = new StringInput("abc");
        stringInput.MoveNext();
        stringInput.MoveNext();
        stringInput.MoveNext();

        var end = stringInput.End;
        var currentLocation = stringInput.CurrentLocation;

        Assert.True(end.Equals(currentLocation));
    }

    [Fact]
    public void ResetSetsBackToStart()
    {
        IInput stringInput = new StringInput("abc");
        stringInput.MoveNext();
        stringInput.MoveNext();
        stringInput.MoveNext();

        stringInput.Reset();

        Assert.Equal(stringInput.Start, stringInput.CurrentLocation);
    }

    [Fact]
    public void MoveToChangesLocation()
    {
        IInput stringInput = new StringInput("abc");
        stringInput.MoveNext();
        var targetLocation = stringInput.CurrentLocation;
        stringInput.MoveNext();
        stringInput.MoveNext();

        stringInput.MoveTo(targetLocation);

        Assert.Equal('a', stringInput.Current);
    }

    [Fact]
    public void MoveToWithIncorrectLocationThrowsArgumentException()
    {
        IInput stringInput = new StringInput("abc");
        ILocation fakeLocation = new FakeLocation();

        Assert.Throws<ArgumentException>(() => stringInput.MoveTo(fakeLocation));
    }

    [Fact]
    public void LocationToStringContainsIndex()
    {
        IInput stringInput = new StringInput("abcde");
        stringInput.MoveNext();
        stringInput.MoveNext();
        stringInput.MoveNext();

        var s = stringInput.CurrentLocation.ToString();

        Assert.Contains("2", s);
    }
}
