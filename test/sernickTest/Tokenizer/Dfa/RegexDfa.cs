namespace sernickTest.Tokenizer.Dfa;

using sernick.Tokenizer.Dfa;
using sernick.Tokenizer.Regex;

public class RegexDfaTest
{
    [Fact]
    public void RegexDfaStartGetter()
    {
        var regex = Regex.Atom('x');
        var regexDfa = new RegexDfa(regex);
        Assert.Equal(regexDfa.Start, regex);
    }

    private static RegexDfa exampleDfa()
    {
        var atomA = Regex.Atom('a');
        var atomB = Regex.Atom('b');
        var atomC = Regex.Atom('c');
        var atomZ = Regex.Atom('z');

        var starZ = Regex.Star(atomZ);
        var acceptLoop = Regex.Concat(atomA, atomB, starZ, atomC);
        var loopStarred = Regex.Star(acceptLoop);

        var pathA = Regex.Concat(atomA, loopStarred);
        var pathB = Regex.Concat(atomB, starZ, atomC, loopStarred);

        var regex = Regex.Union(pathA, pathB);
        var regexDfa = new RegexDfa(regex);
        return regexDfa;
    }

    [Fact]
    public void NonAcceptingState()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Start;
        state = regexDfa.Transition(state, 'b');
        state = regexDfa.Transition(state, 'z');
        state = regexDfa.Transition(state, 'z');

        Assert.False(regexDfa.Accepts(state));
    }

    [Fact]
    public void NonAcceptingState2()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Start;
        state = regexDfa.Transition(state, 'b');
        state = regexDfa.Transition(state, 'c');
        state = regexDfa.Transition(state, 'b');

        Assert.False(regexDfa.Accepts(state));
    }

    [Fact]
    public void AcceptingState()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Start;
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
        var regexDfa = exampleDfa();
        var state = regexDfa.Start;
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'b');
        state = regexDfa.Transition(state, 'c');

        Assert.True(regexDfa.Accepts(state));
    }

    [Fact]
    public void NonDeadState()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Start;
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'b');
        state = regexDfa.Transition(state, 'c');

        Assert.False(regexDfa.IsDead(state));
    }

    [Fact]
    public void NonDeadState2()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Start;
        Assert.False(regexDfa.IsDead(state));
    }

    [Fact]
    public void DeadState()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Start;

        state = regexDfa.Transition(state, 'x');

        Assert.True(regexDfa.IsDead(state));
    }

    [Fact]
    public void DeadState2()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Start;

        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'a');
        state = regexDfa.Transition(state, 'a');

        Assert.True(regexDfa.IsDead(state));
    }
}
