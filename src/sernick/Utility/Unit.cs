namespace sernick.Utility;

/// <summary>
/// Unit type, for when the generics are irrelevant and nothing matters anymore
/// </summary>
public sealed record Unit
{
    public static readonly Unit I = new Unit();
}
