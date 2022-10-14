namespace sernick.Input.String;

public class StringLocation : ILocation
{
    public StringLocation(int index)
    {
        Index = index;
    }

    public int Index { get; }

    public override string ToString() => $"character index {Index}";

    private bool Equals(StringLocation other) => Index == other.Index;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((StringLocation)obj);
    }

    public override int GetHashCode() => Index.GetHashCode();
}
