namespace sernick.Input.String;

public class StringLocation : ILocation
{
    public StringLocation(int index)
    {
        Index = index;
    }

    public int Index { get; }

    public override string ToString() => $"character index {Index}";

    public override bool Equals(object? obj) => obj is StringLocation location && Equals(location);

    private bool Equals(StringLocation other) => Index == other.Index;

    public override int GetHashCode() => Index.GetHashCode();
}
