namespace sernickTest.Utility;

using sernick.Utility;

public class FileUtilityTest
{
    [Fact]
    public void GetFirstLetters()
    {
        const string FILE_NAME = "examples/argument-types/correct/multiple-args.ser";
        var file = File.ReadAllText(FILE_NAME);
        var input = FILE_NAME.ReadFile();

        Assert.Equal(file[0], input.Current);
        Assert.Equal(input.Start, input.CurrentLocation);

        var result = input.MoveNext();
        Assert.True(result);
        Assert.Equal(file[1], input.Current);
        Assert.NotEqual(input.CurrentLocation, input.Start);
    }

    [Fact]
    public void AtTheEndOfFile()
    {
        const string FILE_NAME = "examples/argument-types/correct/multiple-args.ser";
        var input = FILE_NAME.ReadFile();
        input.MoveTo(input.End);

        Assert.Equal(input.End, input.CurrentLocation);

        var result = input.MoveNext();
        Assert.False(result);
        Assert.Equal(input.End, input.CurrentLocation);
    }

    [Fact]
    public void MovingBack()
    {
        const string FILE_NAME = "examples/argument-types/correct/multiple-args.ser";
        var file = File.ReadAllText(FILE_NAME);
        var input = FILE_NAME.ReadFile();

        var result = input.MoveNext();
        var location = input.CurrentLocation;

        Assert.True(result);
        Assert.Equal(file[1], input.Current);

        input.MoveNext();
        input.MoveNext();
        result = input.MoveNext();

        Assert.True(result);
        Assert.Equal(file[4], input.Current);
        Assert.NotEqual(input.CurrentLocation, location);

        input.MoveTo(location);
        Assert.Equal(file[1], input.Current);
        Assert.Equal(location, input.CurrentLocation);
    }
}
