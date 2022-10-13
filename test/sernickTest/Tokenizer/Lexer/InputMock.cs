using System.Collections;
using sernick.Input;

namespace sernickTest.Tokenizer.Lexer;

internal class InputMock : IInput
{
    private readonly string input;
    private Location currentLocation;

    public InputMock(string input)
    {
        this.input = input;
        currentLocation = new Location(0);
    }

    internal record Location(int position) : ILocation { }

    public ILocation CurrentLocation => currentLocation;

    public ILocation Start => new Location(0);

    public ILocation End => new Location(input.Length);

    public char Current => input[currentLocation.position];

    object IEnumerator.Current => Current;

    public void Dispose()
    { }

    public bool MoveNext()
    {
        if (currentLocation != (Location)End)
        {
            currentLocation = new Location(currentLocation.position + 1);
        }

        return currentLocation != (Location)End;
    }

    public void MoveTo(ILocation location)
    {
        currentLocation = (Location)location;
    }

    public void Reset()
    {
        currentLocation = (Location)Start;
    }
}
