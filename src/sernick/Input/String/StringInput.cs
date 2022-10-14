using System.Collections;

namespace sernick.Input.String;

public class StringInput : IInput
{
    private readonly string _text;

    public StringInput(string text)
    {
        _text = text;
        Start = (-1).Pack();
        End = (text.Length - 1).Pack();
        CurrentLocation = Start;
        Current = (char)0;
    }

    public char Current { get; private set; }

    object IEnumerator.Current => Current;

    public ILocation CurrentLocation { get; private set; }

    /// <summary>
    ///     Points to the location before the first character
    /// </summary>
    public ILocation Start { get; }

    /// <summary>
    ///     Points to the location of the last character
    /// </summary>
    public ILocation End { get; }

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
        MoveTo(CurrentLocation.Next());
        return HasFinished();
    }

    public void Reset() => MoveTo(Start);

    public void MoveTo(ILocation location)
    {
        CurrentLocation = location;
        Current = CharAtLocation(CurrentLocation);
    }

    private char CharAtLocation(ILocation location)
    {
        var position = location.Unpack();
        if (0 <= position && position <= _text.Length - 1)
        {
            return _text[position];
        }

        return (char)0;
    }

    private bool HasFinished() => CurrentLocation.Equals(End);
}
