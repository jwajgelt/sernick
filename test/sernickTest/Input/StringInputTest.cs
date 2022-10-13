using sernick.Input;

namespace sernickTest.Input;

public class StringInputTest
{
    [Fact]
    public void EmptyString()
    {
        // Arrange
        IInput stringInput = new StringInput("");

        // Act
        var currentBefore = stringInput.Current;
        var move = stringInput.MoveNext();
        var currentAfter = stringInput.Current;

        // Assert
        Assert.Equal(0, currentBefore);
        Assert.False(move);
        Assert.Equal(0, currentAfter);
    }

    [Fact]
    public void BasicString()
    {
        // Arrange
        IInput stringInput = new StringInput("ab");

        // Act
        var currentBefore = stringInput.Current;
        var move0 = stringInput.MoveNext();
        var current0 = stringInput.Current;
        var move1 = stringInput.MoveNext();
        var current1 = stringInput.Current;
        var move2 = stringInput.MoveNext();
        var currentAfter = stringInput.Current;

        // Assert
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
        // Arrange
        IInput stringInput = new StringInput("abc");

        // Act
        var start = stringInput.Start;
        var currentLocation = stringInput.CurrentLocation;

        // Assert
        Assert.True(start.Equals(currentLocation));
    }

    [Fact]
    public void EndEqualsCurrentAfterIterating()
    {
        // Arrange
        IInput stringInput = new StringInput("abc");
        stringInput.MoveNext();
        stringInput.MoveNext();
        stringInput.MoveNext();

        // Act
        var end = stringInput.End;
        var currentLocation = stringInput.CurrentLocation;

        // Assert
        Assert.True(end.Equals(currentLocation));
    }

    [Fact]
    public void ResetSetsBackToStart()
    {
        // Arrange
        IInput stringInput = new StringInput("abc");
        stringInput.MoveNext();
        stringInput.MoveNext();
        stringInput.MoveNext();

        // Act
        stringInput.Reset();

        // Assert
        Assert.Equal(stringInput.Start, stringInput.CurrentLocation);
    }

    [Fact]
    public void MoveToChangesLocation()
    {
        // Arrange
        IInput stringInput = new StringInput("abc");
        stringInput.MoveNext();
        var targetLocation = stringInput.CurrentLocation;
        stringInput.MoveNext();
        stringInput.MoveNext();

        // Act
        stringInput.MoveTo(targetLocation);

        // Assert
        Assert.Equal('a', stringInput.Current);
    }

    [Fact]
    public void MoveToWithIncorrectLocationThrowsArgumentException()
    {
        // Arrange
        IInput stringInput = new StringInput("abc");
        ILocation fakeLocation = new FakeLocation();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => stringInput.MoveTo(fakeLocation));
    }
    
    [Fact]
    public void LocationToStringContainsIndex()
    {
        // Arrange
        IInput stringInput = new StringInput("abcde");
        stringInput.MoveNext();
        stringInput.MoveNext();
        stringInput.MoveNext();

        // Act
        var s = stringInput.CurrentLocation.ToString();

        // Assert
        Assert.Contains("2", s);
    }
}
