using System.Text;

namespace sernick.Tokenizer.Regex;
/// <summary>
/// [[:lower]abc]*abc
/// a$b$c*$d*
/// abc*d*$$$
/// (a+b)*(c+d)+(e*f)
/// ab+cd+*ef*+
/// </summary>
public static class StringToRegex
{
    private const char ConcatenationOperator = '\0';
    private const char UnionOperator = '|';
    private const char StarOperator = '*';
    private const char PlusOperator = '+';
    private static readonly HashSet<char> s_operators = new HashSet<char>() { '+', '*', '|' };

    private static string AddConcatenationOperator(this string text)
    {
        var stringBuilder = new StringBuilder();
        var bracketCounter = 0;

        for (var index = 0; index < text.Length - 1; index++)
        {
            var left = text[index];
            var right = text[index + 1];
            if (left == '[')
            {
                bracketCounter++;
            }
            else if (left == ']')
            {
                bracketCounter--;
            }

            stringBuilder.Append(left);
            if (bracketCounter > 0 || s_operators.Contains(right) || left is '|' or '(' || right == ')')
            {
                continue;
            }

            stringBuilder.Append(ConcatenationOperator);
        }

        stringBuilder.Append(text[^1]);
        return stringBuilder.ToString();
    }
    private static Regex Helper(this string s)
    {
        if (s.Length == 1)
        {
            return Regex.Atom(s[0]);
        }

        if (s[0] != '[')
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

                children.Add(s_characterClasses.GetValueOrDefault(s.Substring(start, index - start + 1), Regex.Empty));
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

    private static readonly Dictionary<string, Regex> s_characterClasses = new Dictionary<string, Regex>()
    {
        ["[:lower:]"] = Range('a', 'z'),
        ["[:upper:]"] = Range('A', 'Z'),
        ["[:space:]"] = Regex.Union(" \t\n\r\f\v".ToList().ConvertAll(atom => new AtomRegex(atom))),
        ["[:alnum:]"] = Regex.Union(new[] { Range('a', 'z'), Range('A', 'Z'), Range('0', '9') }),
        ["[:digit:]"] = Range('0', '9')
    };

    private static readonly Dictionary<char, int> s_priorities = new Dictionary<char, int>()
    {
        [')'] = 0,
        ['|'] = 1,
        [ConcatenationOperator] = 2,
        ['+'] = 3,
        ['*'] = 3,
        ['('] = 4
    };

    public static Regex ToRegex(this string text)
    {
        var operators = new Stack<char>();
        var resultStack = new Stack<Regex>();

        var current = "";
        var escaped = false;

        var textWithConcatenationOperator = text.AddConcatenationOperator();
        Console.WriteLine(textWithConcatenationOperator);

        foreach (var t in textWithConcatenationOperator)
        {
            if (t == '\\')
            {
                escaped = true;
                continue;
            }

            if (escaped || !s_priorities.ContainsKey(t))
            {
                current += t;
                escaped = false;
                continue;
            }

            if (current != "")
            {
                resultStack.Push(current.Helper());
                current = "";
            }

            var priority = s_priorities[t];

            while (operators.Count > 0 && operators.Peek() != '(' && priority < s_priorities[operators.Peek()])
            {
                var top = resultStack.Pop();
                switch (operators.Pop())
                {
                    case UnionOperator:
                        resultStack.Push(Regex.Union(new[] { resultStack.Pop(), top }));
                        break;
                    case ConcatenationOperator:
                        resultStack.Push(Regex.Concat(new[] { resultStack.Pop(), top }));
                        break;
                    case StarOperator:
                        resultStack.Push(Regex.Star(top));
                        break;
                    case PlusOperator:
                        resultStack.Push(Regex.Concat(new[] { top, Regex.Star(top) }));
                        break;
                }
            }

            if (t == ')')
            {
                operators.Pop();
            }
            else
            {
                operators.Push(t);
            }
        }

        if (current != "")
        {
            resultStack.Push(current.Helper());
        }

        while (operators.Count > 0)
        {
            var top = resultStack.Pop();
            switch (operators.Pop())
            {
                case UnionOperator:
                    resultStack.Push(Regex.Union(new[] { resultStack.Pop(), top }));
                    break;
                case ConcatenationOperator:
                    resultStack.Push(Regex.Concat(new[] { resultStack.Pop(), top }));
                    break;
                case StarOperator:
                    resultStack.Push(Regex.Star(top));
                    break;
                case PlusOperator:
                    resultStack.Push(Regex.Concat(new[] { top, Regex.Star(top) }));
                    break;
            }
        }

        if (resultStack.Count > 1)
        {
            throw new Exception();
        }

        return resultStack.Count == 1 ? resultStack.Peek() : Regex.Empty;
    }
}
