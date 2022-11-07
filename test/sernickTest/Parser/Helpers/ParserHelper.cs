namespace sernickTest.Parser.Helpers;

using sernick.Grammar.Lexicon;

public sealed record CharCategory(char Character) : LexicalGrammarCategory(Priority: 1)
{
    public override string ToString() => Character.ToString();
}

public static class CharCategoryHelper
{
    public static CharCategory ToCategory(this char character) => new(character);
}
