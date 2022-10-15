namespace sernick.Input.String;

public class StringInput : IInput
{
    private readonly string _text;

    public StringInput(string text)
    {
        _text = text;
        Start = (0).Pack();
        End = (_text.Length).Pack();
        CurrentLocation = Start;
        Current = null;
        if (_text.Length > 0)
        {
            Current = _text[0];
        }
    }

    public char? Current { get; private set; }

    public ILocation CurrentLocation { get; private set; }

    public ILocation Start { get; }

    public ILocation End { get; }

    public bool MoveNext()
    {
        MoveTo(CurrentLocation.Next());
        return CurrentLocation.Unpack() < End.Unpack();
    }

    public void MoveTo(ILocation location)
    {
        CurrentLocation = location;
        Current = CharAtLocation(CurrentLocation);
    }

    private char? CharAtLocation(ILocation location)
    {
        var position = location.Unpack();
        if (0 <= position && position <= _text.Length - 1)
        {
            return _text[position];
        }

        return null;
    }
}
