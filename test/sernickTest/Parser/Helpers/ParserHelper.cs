namespace sernickTest.Parser.Helpers;

public sealed record CharCategory(char Character)
{
    public override string ToString() => Character.ToString();
}

public static class CharCategoryHelper
{
    public static CharCategory ToCategory(this char character) => new(character);
}
