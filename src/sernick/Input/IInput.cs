namespace sernick.Input;

public interface IInput : IEnumerator<char>
{
    void MoveTo(ILocation location);
    ILocation CurrentLocation { get; }
    ILocation Start { get; }
    ILocation End { get; }
}
