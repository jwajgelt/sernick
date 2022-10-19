namespace sernickTest.Tokenizer.Lexer.Helpers;

using sernick.Input;

internal sealed class FakeInput : IInput
{
    private readonly string _input;
    private Location _currentLocation;

    public FakeInput(string input) => (_input, _currentLocation) = (input, new Location(-1));

    internal sealed record Location(int Position) : ILocation;

    public ILocation CurrentLocation => _currentLocation;

    public ILocation Start => new Location(0);

    public ILocation End => new Location(_input.Length);

    public char Current => _input[_currentLocation.Position];

    public bool MoveNext()
    {
        if (_currentLocation != (Location)End)
        {
            _currentLocation = new Location(_currentLocation.Position + 1);
        }

        return _currentLocation != (Location)End;
    }

    public void MoveTo(ILocation location)
    {
        _currentLocation = (Location)location;
    }
}
