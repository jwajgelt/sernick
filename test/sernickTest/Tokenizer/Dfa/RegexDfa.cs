namespace sernickTest.Tokenizer.Dfa;

using Helpers;
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

    /*
        exampleDfa should return a DFA with 4 states (or equivalent):
        - state 1 (starting state)
            transition to state 2 by 'a'
            transition to state 4 by 'b'
        - state 2 (accepting)
            transition to state 3 by 'a'
        - state 3 
            transition to state 4 by 'b'
        - state 4
            transition to state 2 by 'c'
            transition to state 4 by 'z'


                         -------- c ----------
                        /                     \   ---z---
                       V                       \ /       \
        (1)--- a --->[2]--- a --->(3)--- b --->(4)<--------
          \                                    ^                                    
           \                                  /
            ---------------- b ---------------


        Which is equivalent to regex: (a(ab(z*)c)*)|(b(z*)c(ab(z*)c)*)
     */
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
        var state = regexDfa.Transition(regexDfa.Start, "bzz");

        Assert.False(regexDfa.Accepts(state));
    }

    [Fact]
    public void NonAcceptingState2()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Transition(regexDfa.Start, "bcb");

        Assert.False(regexDfa.Accepts(state));
    }

    [Fact]
    public void AcceptingState()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Transition(regexDfa.Start, "bcabzzc");

        Assert.True(regexDfa.Accepts(state));
    }

    [Fact]
    public void AcceptingState2()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Transition(regexDfa.Start, "aabc");

        Assert.True(regexDfa.Accepts(state));
    }

    [Fact]
    public void NonDeadState()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Transition(regexDfa.Start, "aabc");

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
        var state = regexDfa.Transition(regexDfa.Start, "x");

        Assert.True(regexDfa.IsDead(state));
    }

    [Fact]
    public void DeadState2()
    {
        var regexDfa = exampleDfa();
        var state = regexDfa.Transition(regexDfa.Start, "aaa");

        Assert.True(regexDfa.IsDead(state));
    }
}
