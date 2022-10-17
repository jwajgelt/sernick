namespace sernick.Input.String;

internal static class StringInputExtensionMethods
{
    public static int Unpack(this ILocation location)
    {
        if (location is StringLocation stringLocation)
        {
            return stringLocation.Index;
        }

        throw new ArgumentException("Location provided did not originate in this Input");
    }

    public static ILocation Pack(this int index) => new StringLocation(index);

    public static ILocation Next(this ILocation location) => (location.Unpack() + 1).Pack();
}
