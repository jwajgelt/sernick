namespace sernick.Input;

public interface IInput : IEnumerable<char>
{
    void MoveTo(ILocation location);
    ILocation CurrentLocation { get; }
    ILocation Start { get; }
    ILocation End { get; }
}
