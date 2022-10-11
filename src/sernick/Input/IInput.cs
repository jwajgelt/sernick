namespace sernick.Input;

public interface IInput: IEnumerable<Char>
{
    void MoveTo(ILocation location);
    ILocation CurrentLocation{ get; }
    ILocation Start{ get; }
    ILocation End{ get; }
}
