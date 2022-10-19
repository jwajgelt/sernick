namespace sernick.Tokenizer.Regex;

using Common.Regex;

public static class StringToRegex
{
    /// <summary>
    /// Creates a Regex object from a string using the shunting yard algorithm.
    /// The input string must follow the POSIX standard for regexes. As of now the following features are implemented:
    /// <list type="bullet">
    ///     <item>metacharacters: *, +, ., ()</item>
    ///     <item>
    ///     character classes:
    ///     <list type="bullet">
    ///         <item><c>[[:lower:]]</c></item>
    ///         <item><c>[[:upper:]]</c></item>
    ///         <item><c>[[:space:]]</c></item>
    ///         <item><c>[[:alnum:]]</c></item>
    ///         <item><c>[[:digit:]]</c></item>
    ///         <item><c>[[:any:]]</c> - custom class equivalent to the <c>'.'</c> metacharacter</item>
    ///     </list>
    /// </item>
    /// </list>
    /// </summary>
    public static Regex ToRegex(this string text)
    {
        var specialCharactersStack = new Stack<SpecialCharacter>();
        var resultStack = new Stack<Regex>();

        var tokenizedText = text.Tokenize().AddConcatenation();

        foreach (var token in tokenizedText)
        {

            // if token is not a special character then add it to the result and move to the next token
            if (!token.IsSpecial)
            {
                resultStack.Push(Regex.Atom(token.Value[0]));
                continue;
            }

            // if token is a character class then add it to the result and move to the next token
            if (CharacterClasses.ContainsKey(token.Value))
            {
                resultStack.Push(CharacterClasses[token.Value]);
                continue;
            }

            // a special token which is not a character class should be special character
            if (!token.IsSpecialCharacter())
            {
                throw new ArgumentException($"Error while parsing {token.Value}");
            }

            var specialCharacter = (SpecialCharacter)token.Value[0];

            var priority = Priorities[specialCharacter];

            // pop from the stack all the special characters until one with a priority lower or equal
            // to the current token's priority or an opening parenthesis is encountered
            while (specialCharactersStack.Count > 0 &&
                   priority < Priorities[specialCharactersStack.Peek()] &&
                   specialCharactersStack.Peek() != SpecialCharacter.LeftParenthesis
                  )
            {
                HandleSpecialCharacter(resultStack, specialCharactersStack);
            }

            // if the current token was a closing parenthesis
            // then there should be an opening one on the stack that need to be popped
            if (specialCharacter == SpecialCharacter.RightParenthesis)
            {
                if (specialCharactersStack.Count == 0 ||
                    specialCharactersStack.Peek() != SpecialCharacter.LeftParenthesis)
                {
                    throw new ArgumentException($"Error while parsing {text}");
                }

                specialCharactersStack.Pop();
            }
            // otherwise the token is an operator that needs to be pushed to the stack
            else
            {
                specialCharactersStack.Push(specialCharacter);
            }
        }

        while (specialCharactersStack.Count > 0)
        {
            HandleSpecialCharacter(resultStack, specialCharactersStack);
        }

        // after the entire algorithm there should be only one final Regex on the stack
        // if that's not the case it means the input text must have been invalid
        if (resultStack.Count > 1)
        {
            throw new ArgumentException(nameof(resultStack));
        }

        return resultStack.Count == 1 ? resultStack.Peek() : Regex.Empty;
    }

    // special <-> is a metacharacter (special character) or a character class
    private record struct Token(string Value, bool IsSpecial);

    private static bool IsSpecialCharacter(this Token token)
    {
        return token.IsSpecial && token.Value.Length == 1 &&
               Enum.IsDefined(typeof(SpecialCharacter), (int)token.Value[0]);
    }

    private enum SpecialCharacter
    {
        Concatenation = '\0',
        Union = '|',
        Star = '*',
        Plus = '+',
        LeftParenthesis = '(',
        RightParenthesis = ')'
    }

    private static char ToChar(this SpecialCharacter specialCharacter) => (char)specialCharacter;

    private static List<Token> Tokenize(this string text)
    {
        var result = new List<Token>();

        // iterate with index so I can skip easily
        for (var index = 0; index < text.Length; index++)
        {
            var t = text[index];

            Token token;

            switch (t)
            {
                // if escaped create a non-special (even if the character is special) token
                // containing just the character after the backslash
                case '\\':
                    index++;
                    token = new Token(text[index].ToString(), false);
                    break;
                // if starts with an opening bracket check for a character class and create a special token
                case '[':
                    {
                        var temp = index;
                        while (text[temp] != ']')
                        {
                            temp++;
                        }

                        token = new Token(text.Substring(index, temp - index + 2), true);
                        if (!CharacterClasses.ContainsKey(token.Value))
                        {
                            throw new ArgumentException($"{token.Value} is not a valid CharacterClass");
                        }

                        index = temp + 1;
                        break;
                    }
                // if '.' create a special any character class token
                case '.':
                    token = new Token("[[:any:]]", true);
                    break;
                // otherwise create a token that is special iff the current character is a special character
                default:
                    token = new Token(t.ToString(), Enum.IsDefined(typeof(SpecialCharacter), (int)t));
                    break;
            }

            result.Add(token);
        }

        return result;
    }

    private static List<Token> AddConcatenation(this List<Token> text)
    {
        var result = new List<Token>();
        var concatenationToken = new Token(SpecialCharacter.Concatenation.ToChar().ToString(), true);

        // iterate over every space between two tokens to check if there should be a concat character
        foreach (var (left, right) in text.Zip(text.Skip(1)))
        {
            result.Add(left);

            // if the right character is a special character other than an opening parenthesis then
            // don't add a concat character
            if (right.IsSpecialCharacter() && (SpecialCharacter)right.Value[0] != SpecialCharacter.LeftParenthesis)
            {
                continue;
            }

            // if the left character is a union character or an opening parenthesis then
            // don't add a concat character
            if (left.IsSpecialCharacter() &&
                (SpecialCharacter)left.Value[0] is SpecialCharacter.Union or SpecialCharacter.LeftParenthesis)
            {
                continue;
            }

            result.Add(concatenationToken);
        }

        result.Add(text[^1]);
        return result;
    }

    private static Regex Range(char start, char end)
    {
        return Regex.Union(Enumerable.Range(start, end - start + 1).Select(atom => Regex.Atom((char)atom)));
    }

    private static readonly Dictionary<string, Regex> CharacterClasses = new()
    {
        ["[[:lower:]]"] = Range('a', 'z'),
        ["[[:upper:]]"] = Range('A', 'Z'),
        ["[[:space:]]"] = Regex.Union(" \t\n\r\f\v".Select(Regex.Atom)),
        ["[[:alnum:]]"] = Regex.Union(Range('a', 'z'), Range('A', 'Z'), Range('0', '9')),
        ["[[:digit:]]"] = Range('0', '9'),
        ["[[:any:]]"] = Range(' ', '~')
    };

    private static readonly Dictionary<SpecialCharacter, int> Priorities = new()
    {
        [SpecialCharacter.RightParenthesis] = 0,
        [SpecialCharacter.Union] = 1,
        [SpecialCharacter.Concatenation] = 2,
        [SpecialCharacter.Plus] = 3,
        [SpecialCharacter.Star] = 3,
        [SpecialCharacter.LeftParenthesis] = 4
    };

    private static void HandleSpecialCharacter(Stack<Regex> resultStack, Stack<SpecialCharacter> operatorsStack)
    {
        var top = resultStack.Pop();
        resultStack.Push(operatorsStack.Pop() switch
        {
            SpecialCharacter.Union => Regex.Union(resultStack.Pop(), top),
            SpecialCharacter.Concatenation => Regex.Concat(resultStack.Pop(), top),
            SpecialCharacter.Star => Regex.Star(top),
            SpecialCharacter.Plus => Regex.Concat(top, Regex.Star(top)),
            _ => throw new ArgumentOutOfRangeException(nameof(resultStack))
        });
    }
}
