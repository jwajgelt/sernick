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
    }

    public char Current
    {
        get
        {
            var current = GetCurrentChar();
            if (current.HasValue)
            {
                return current.Value;
            }

            throw new InvalidOperationException("The Input does not point to a character");
        }
    }

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
        // verify correct type
        location.Unpack();

        CurrentLocation = location;
    }

    private char? GetCurrentChar()
    {
        var position = CurrentLocation.Unpack();
        if (0 <= position && position <= _text.Length - 1)
        {
            return _text[position];
        }

        return null;
    }
}
