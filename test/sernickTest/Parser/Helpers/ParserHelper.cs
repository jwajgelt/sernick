namespace sernickTest.Parser.Helpers;

using sernick.Grammar.Lexicon;

public record CharCategory(char Character): ILexicalGrammarCategory
{
    public short Priority => 1;
}

public static class CharCategoryHelper
{
    public static CharCategory ToCategory(this char character) => new (character);
}
