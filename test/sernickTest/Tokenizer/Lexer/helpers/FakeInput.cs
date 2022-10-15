using System.Collections;
using sernick.Input;

namespace sernickTest.Tokenizer.Lexer.Helpers;

internal class FakeInput : IInput
{
    private readonly string _input;
    private Location _currentLocation;

    public FakeInput(string input)
    {
        _input = input;
        _currentLocation = new Location(-1);
    }

    internal record Location(int position) : ILocation { }

    public ILocation CurrentLocation => _currentLocation;

    public ILocation Start => new Location(0);

    public ILocation End => new Location(_input.Length);

    public char Current => _input[_currentLocation.position];

    object IEnumerator.Current => Current;

    public void Dispose()
    { }

    public bool MoveNext()
    {
        if (_currentLocation != (Location)End)
        {
            _currentLocation = new Location(_currentLocation.position + 1);
        }

        return _currentLocation != (Location)End;
    }

    public void MoveTo(ILocation location)
    {
        _currentLocation = (Location)location;
    }

    public void Reset()
    {
        _currentLocation = (Location)Start;
    }
}
