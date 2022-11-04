namespace sernickTest.Common.Dfa;

using sernick.Common.Dfa;
using Tokenizer.Lexer.Helpers;

using SumDfaState = sernick.Common.Dfa.SumDfa<int, int, char>.State;

public class SumDfaTest
{
    [Fact]
    public void ReturnsCorrectTransitionsFrom()
    {
        var transitions1 = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 },
            { (1, 'a'), 2 }
        };
        var dfa1 = new FakeDfa(
            transitions1,
            0,
            new HashSet<int> { 2 }
        );
        var transitions2 = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 },
            { (1, 'b'), 2 }
        };
        var dfa2 = new FakeDfa(
            transitions2,
            0,
            new HashSet<int> { 2 }
        );

        var sumDfa = new SumDfa<int, int, char>(new Dictionary<int, IDfa<int, char>> { { 0, dfa1 }, { 1, dfa2 } });

        var transitionsFromStart = sumDfa.GetTransitionsFrom(sumDfa.Start).ToList();
        Assert.Single(transitionsFromStart);
        Assert.Equal(sumDfa.Transition(sumDfa.Start, 'a'), transitionsFromStart[0].To);

        var state = new SumDfaState(new Dictionary<int, int> { { 0, 1 }, { 1, 1 } });

        var transitionsFromState = sumDfa.GetTransitionsFrom(state).ToList();

        Assert.Equal(2, transitionsFromState.Count);
        foreach (var transition in transitionsFromState)
        {
            var actualTo = sumDfa.Transition(state, transition.Atom);
            Assert.Equal(actualTo, transition.To);
        }
    }

    [Fact]
    public void ReturnsCorrectTransitionsTo()
    {
        var transitions1 = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 },
            { (1, 'a'), 2 }
        };
        var dfa1 = new FakeDfa(
            transitions1,
            0,
            new HashSet<int> { 2 }
        );
        var transitions2 = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 },
            { (1, 'b'), 2 }
        };
        var dfa2 = new FakeDfa(
            transitions2,
            0,
            new HashSet<int> { 2 }
        );

        var sumDfa = new SumDfa<int, int, char>(new Dictionary<int, IDfa<int, char>> { { 0, dfa1 }, { 1, dfa2 } });

        var state = sumDfa.Transition(sumDfa.Start, 'a'); //new Dictionary<int, int> { { 0, 1 }, { 1, 1 } };

        var transitionsToState = sumDfa.GetTransitionsTo(state).ToList();

        Assert.Single(transitionsToState);
        Assert.Equal(sumDfa.Start, transitionsToState[0].From);
    }

    [Fact]
    public void ReturnsCorrectAcceptingStates()
    {
        var transitions1 = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 },
            { (1, 'a'), 2 }
        };
        var dfa1 = new FakeDfa(
            transitions1,
            0,
            new HashSet<int> { 2 }
        );
        var transitions2 = new Dictionary<(int, char), int>
        {
            { (0, 'a'), 1 },
            { (1, 'b'), 2 }
        };
        var dfa2 = new FakeDfa(
            transitions2,
            0,
            new HashSet<int> { 2 }
        );

        var sumDfa = new SumDfa<int, int, char>(new Dictionary<int, IDfa<int, char>> { { 0, dfa1 }, { 1, dfa2 } });
        var acceptingStates = sumDfa.AcceptingStates;

        var state = sumDfa.Transition(sumDfa.Start, 'a');
        var actualAcceptingStates = new List<SumDfaState>
        {
            sumDfa.Transition(state, 'a'), sumDfa.Transition(state, 'b')
        };

        Assert.Equal(actualAcceptingStates, acceptingStates);
    }
}
