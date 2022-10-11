using System.Collections;

namespace sernick.Input;

public class StringInput : IInput
{
    public StringInput(string text)
    {
        throw new NotImplementedException();
    }
    public IEnumerator<char> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void MoveTo(ILocation location)
    {
        throw new NotImplementedException();
    }

    public ILocation CurrentLocation { get; }
    public ILocation Start { get; }
    public ILocation End { get; }
}