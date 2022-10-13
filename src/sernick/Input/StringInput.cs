using System.Collections;

namespace sernick.Input;

public class StringInput : IInput
{
    public StringInput(string text)
    {
        throw new NotImplementedException();
    }

    public bool MoveNext()
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void MoveTo(ILocation location)
    {
        throw new NotImplementedException();
    }

    public char Current { get; }
    object IEnumerator.Current => Current;
    public ILocation CurrentLocation { get; }
    public ILocation Start { get; }
    public ILocation End { get; }
}
