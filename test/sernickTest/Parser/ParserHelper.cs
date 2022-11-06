namespace sernickTest.Parser;

using sernick.Grammar.Lexicon;

public class CharCategory : ILexicalGrammarCategory, IEquatable<CharCategory>
{
    public CharCategory(char character)
    {
        Character = character;
    }

    public readonly char Character;
    public short Priority => 1;

    public bool Equals(CharCategory? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Character == other.Character;
    }

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

        return obj.GetType() == GetType() && Equals((CharCategory)obj);
    }

    public override int GetHashCode()
    {
        return Character.GetHashCode();
    }
}

public static class CharCategoryHelper
{
    public static CharCategory ToCategory(this char character)
    {
        return new CharCategory(character);
    }
}
