namespace sernick.Input;

public interface IInput
{
    /// <summary>
    ///     Advances input to the next element.
    ///     Upon creation input is positioned on the Start location.
    /// </summary>
    /// <returns>
    ///     false if after execution input's location points to End;
    ///     true otherwise 
    /// </returns>
    bool MoveNext();

    void MoveTo(ILocation location);

    char Current { get; }
    ILocation CurrentLocation { get; }

    /// <summary>
    ///     Returns the location of the first char in input.
    /// </summary>
    ILocation Start { get; }

    /// <summary>
    ///     Returns location after the last character in input.
    /// </summary>
    ILocation End { get; }
}
