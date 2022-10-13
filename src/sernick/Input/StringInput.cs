using System.Collections;

namespace sernick.Input;

public class StringInput : IInput
{
    private readonly string _text;

    public StringInput(string text)
    {
        _text = text;
        Start = PackLocation(-1);
        End = PackLocation(text.Length - 1);
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
        var successful = HasNext();
        MoveTo(NextLocation(CurrentLocation));
        return successful;
    }

    public void Reset()
    {
        MoveTo(Start);
    }

    public void MoveTo(ILocation location)
    {
        CurrentLocation = location;
        Current = CharAtLocation(CurrentLocation);
    }

    private char CharAtLocation(ILocation location)
    {
        var position = UnpackLocation(location);
        if (0 <= position && position <= _text.Length - 1) return _text[position];

        return (char)0;
    }

    private bool HasNext()
    {
        return UnpackLocation(CurrentLocation) < UnpackLocation(End);
    }

    private ILocation NextLocation(ILocation location)
    {
        return PackLocation(UnpackLocation(location) + 1);
    }

    private ILocation PackLocation(int location)
    {
        return new StringLocation(location);
    }

    private int UnpackLocation(ILocation location)
    {
        if (location is StringLocation stringLocation)
            return stringLocation.Index;
        throw new ArgumentException("Location provided did not originate in this Input");
    }

    private class StringLocation : ILocation
    {
        public StringLocation(int index)
        {
            Index = index;
        }

        public int Index { get; }

        public override string ToString()
        {
            return $"character index {Index}";
        }

        private bool Equals(StringLocation other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StringLocation)obj);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }
    }
}