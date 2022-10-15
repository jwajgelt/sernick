using System.Text;

namespace sernick.Tokenizer.Regex;
public static class StringToRegex
{
    private const char ConcatenationOperator = '\0';
    private const char UnionOperator = '|';
    private const char StarOperator = '*';
    private const char PlusOperator = '+';
    private static readonly HashSet<char> OperatorsSet = new() { PlusOperator, StarOperator, UnionOperator, ConcatenationOperator };

    private static string AddConcatenationOperator(this string text)
    {
        var stringBuilder = new StringBuilder();
        var bracketCounter = 0;
        var escaped = false;

        for (var index = 0; index < text.Length - 1; index++)
        {
            var left = text[index];
            var right = text[index + 1];

            if (!escaped)
            {
                if (left == '[')
                {
                    bracketCounter++;
                }
                else if (left == ']')
                {
                    bracketCounter--;
                }
            }

            stringBuilder.Append(left);
            if ((!escaped && left == '\\') || bracketCounter > 0 || OperatorsSet.Contains(right) || left is '|' or '(' || right == ')')
            {
                escaped = left == '\\';
                continue;
            }

            escaped = false;
            stringBuilder.Append(ConcatenationOperator);
        }

        stringBuilder.Append(text[^1]);
        return stringBuilder.ToString();
    }
    private static Regex HandleListOrAtom(this string s)
    {
        if (s.Length == 1)
        {
            return Regex.Atom(s[0]);
        }

        if (s[0] != '[' || s[^1] != ']')
        {
            return Regex.Empty;
        }

        var children = new List<Regex>();
        var escaped = false;
        for (var index = 1; index < s.Length - 1; index++)
        {
            if (escaped)
            {
                children.Add(Regex.Atom(s[index]));
                escaped = false;
                continue;
            }

            if (s[index] == '\\')
            {
                escaped = true;
                continue;
            }

            if (s[index] == '[')
            {
                var start = index;
                while (s[index] != ']')
                {
                    index++;
                }

                children.Add(CharacterClasses.GetValueOrDefault(s.Substring(start, index - start + 1), Regex.Empty));
                continue;
            }

            children.Add(Regex.Atom(s[index]));
        }

        return Regex.Union(children);
    }
    private static Regex Range(char start, char end)
    {
        if (start > end)
        {
            return Regex.Empty;
        }

        var children = new List<Regex>();
        for (var atom = start; atom <= end; atom++)
        {
            children.Add(Regex.Atom(atom));
        }

        return Regex.Union(children);
    }

    private static readonly Dictionary<string, Regex> CharacterClasses = new()
    {
        ["[:lower:]"] = Range('a', 'z'),
        ["[:upper:]"] = Range('A', 'Z'),
        ["[:space:]"] = Regex.Union(" \t\n\r\f\v".ToList().ConvertAll(atom => new AtomRegex(atom))),
        ["[:alnum:]"] = Regex.Union(Range('a', 'z'), Range('A', 'Z'), Range('0', '9')),
        ["[:digit:]"] = Range('0', '9'),
        ["[:any:]"] = Range(' ', '~')
    };

    private static readonly Dictionary<char, int> Priorities = new()
    {
        [')'] = 0,
        [UnionOperator] = 1,
        [ConcatenationOperator] = 2,
        [PlusOperator] = 3,
        [StarOperator] = 3,
        ['('] = 4
    };

    private static void HandleOperator(Stack<Regex> resultStack, Stack<char> operatorsStack)
    {
        var top = resultStack.Pop();
        switch (operatorsStack.Pop())
        {
            case UnionOperator:
                resultStack.Push(Regex.Union(resultStack.Pop(), top));
                break;
            case ConcatenationOperator:
                resultStack.Push(Regex.Concat(resultStack.Pop(), top));
                break;
            case StarOperator:
                resultStack.Push(Regex.Star(top));
                break;
            case PlusOperator:
                resultStack.Push(Regex.Concat(top, Regex.Star(top)));
                break;
        }
    }

    /// <summary>
    /// Creates a Regex object from a string using the shunting yard algorithm.
    /// The input string must follow the POSIX standard for regexes. As of now the following features are implemented:
    /// <list type="bullet">
    ///     <item>metacharacters: *, +, ., (), [] (without ranges)</item>
    ///     <item>
    ///     character classes:
    ///     <list type="bullet">
    ///         <item><c>[:lower:]</c></item>
    ///         <item><c>[:upper:]</c></item>
    ///         <item><c>[:space:]</c></item>
    ///         <item><c>[:alnum:]</c></item>
    ///         <item><c>[:digit:]</c></item>
    ///         <item><c>[:any:]</c> - custom class equivalent to the . metacharacter</item>
    ///     </list>
    /// </item>
    /// </list>
    /// </summary>
    public static Regex ToRegex(this string text)
    {
        var operatorsStack = new Stack<char>();
        var resultStack = new Stack<Regex>();

        var current = "";
        var escaped = false;
        var bracketCounter = 0;

        var textWithConcatenationOperator = text.AddConcatenationOperator();
        Console.WriteLine(textWithConcatenationOperator);

        foreach (var t in textWithConcatenationOperator)
        {
            if (!escaped)
            {
                if (t == '[')
                {
                    bracketCounter++;
                }
                else if (t == ']')
                {
                    bracketCounter--;
                }
                else if (t == '.' && bracketCounter == 0)
                {
                    current += "[[:any:]]";
                    escaped = false;
                    continue;
                }
                else if (t == '\\')
                {
                    if (bracketCounter > 0)
                    {
                        current += '\\';
                    }

                    escaped = true;
                    continue;
                }
            }

            if (escaped || !Priorities.ContainsKey(t))
            {
                current += t;
                escaped = false;
                continue;
            }

            if (current != "")
            {
                resultStack.Push(current.HandleListOrAtom());
                current = "";
            }

            var priority = Priorities[t];

            while (operatorsStack.Count > 0 && operatorsStack.Peek() != '(' && priority < Priorities[operatorsStack.Peek()])
            {
                HandleOperator(resultStack, operatorsStack);
            }

            if (t == ')')
            {
                operatorsStack.Pop();
            }
            else
            {
                operatorsStack.Push(t);
            }
        }

        if (current != "")
        {
            resultStack.Push(current.HandleListOrAtom());
        }

        while (operatorsStack.Count > 0)
        {
            HandleOperator(resultStack, operatorsStack);
        }

        return resultStack.Count == 1 ? resultStack.Peek() : Regex.Empty;
    }
}
