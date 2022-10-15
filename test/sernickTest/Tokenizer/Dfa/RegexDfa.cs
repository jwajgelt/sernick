namespace sernickTest.Tokenizer.Dfa;

using sernick.Tokenizer.Regex;
using sernick.Tokenizer.Dfa;

public class RegexDfaTest
{
    [Fact]
    public void RegexDfaStartGetter()
    {
        Regex regex = Regex.Atom('x');
        RegexDfa regexDfa = new RegexDfa(regex);
        Assert.Equal(regexDfa.Start, regex);
    }

    private static RegexDfa exampleDfa()
    {
        Regex atomA = Regex.Atom('a');
        Regex atomB = Regex.Atom('b');
        Regex atomC = Regex.Atom('c');
        Regex atomZ = Regex.Atom('z');

        Regex starZ = Regex.Star(atomZ);
        Regex acceptLoop = Regex.Concat(atomA, atomB, starZ, atomC);
        Regex loopStarred = Regex.Star(acceptLoop);

        Regex pathA = Regex.Concat(atomA, loopStarred);
        Regex pathB = Regex.Concat(atomB, starZ, atomC, loopStarred);

        Regex regex = Regex.Union(pathA, pathB);
        RegexDfa regexDfa = new RegexDfa(regex);
        return regexDfa;
    }

    [Fact]
    public void NonAcceptingState()
    {
        RegexDfa regexDfa = exampleDfa();
        Regex state = regexDfa.Start;
        state = regexDfa.Transition(state, 'b');
        state = regexDfa.Transition(state, 'z');
        state = regexDfa.Transition(state, 'z');

        Assert.False(regexDfa.Accepts(state));
    }

    [Fact]
    public void NonAcceptingState2()
    {
        RegexDfa regexDfa = exampleDfa();
        Regex state = regexDfa.Start;
        state = regexDfa.Transition(state, 'b');
        state = regexDfa.Transition(state, 'c');
        state = regexDfa.Transition(state, 'b');

        Assert.False(regexDfa.Accepts(state));
    }

    [Fact]
    public void AcceptingState()
    {
        RegexDfa regexDfa = exampleDfa();
        Regex state = regexDfa.Start;
        state = regexDfa.Transition(state, 'b');
        state = regexDfa.Transition(state, 'c');
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'b');
        state = regexDfa.Transition(state, 'z');
        state = regexDfa.Transition(state, 'z');
        state = regexDfa.Transition(state, 'c');

        Assert.True(regexDfa.Accepts(state));
    }

    [Fact]
    public void AcceptingState2()
    {
        RegexDfa regexDfa = exampleDfa();
        Regex state = regexDfa.Start;
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'b');
        state = regexDfa.Transition(state, 'c');

        Assert.True(regexDfa.Accepts(state));
    }

    [Fact]
    public void NonDeadState()
    {
        RegexDfa regexDfa = exampleDfa();
        Regex state = regexDfa.Start;
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'b');
        state = regexDfa.Transition(state, 'c');

        Assert.False(regexDfa.IsDead(state));
    }

    [Fact]
    public void NonDeadState2()
    {
        RegexDfa regexDfa = exampleDfa();
        Regex state = regexDfa.Start;
        Assert.False(regexDfa.IsDead(state));
    }

    [Fact]
    public void DeadState()
    {
        RegexDfa regexDfa = exampleDfa();
        Regex state = regexDfa.Start;

        state = regexDfa.Transition(state, 'x');

        Assert.True(regexDfa.IsDead(state));
    }

    [Fact]
    public void DeadState2()
    {
        RegexDfa regexDfa = exampleDfa();
        Regex state = regexDfa.Start;

        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'a');

        Assert.True(regexDfa.IsDead(state));
    }
}