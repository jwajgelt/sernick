namespace sernickTest.CodeGeneration.LivenessAnalysis;

using sernick.CodeGeneration;
using sernick.CodeGeneration.LivenessAnalysis;
using sernick.Compiler.Instruction;
using sernick.ControlFlowGraph.CodeTree;
using Utility;

public class LivenessAnalyzerTest
{
    private static readonly Register x = new();
    private static readonly Register y = new();
    private static readonly Label conditionTarget = new("ConditionTarget");

    private static readonly RegisterValue constant = new(0);

    [Fact]
    public void RegistersDefinedBetweenUsesInterfere()
    {
        var instructions = new List<IInstruction>
        {
            new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(y.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(constant.AsOperand(), x.AsRegOperand()),
            new MovInstruction(constant.AsOperand(), y.AsRegOperand())
        };

        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);

        Assert.Single(interferenceGraph[x], y);
        Assert.Empty(copyGraph[x]);
    }

    [Fact]
    public void UnusedRegistersDefinedBetweenUsesInterfere()
    {
        var instructions = new List<IInstruction>
        {
            new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(y.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(constant.AsOperand(), x.AsRegOperand())
        };

        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);

        Assert.Single(interferenceGraph[x], y);
        Assert.Empty(copyGraph[x]);
    }

    [Fact]
    public void RegistersNotDefinedBetweenUsesDontInterfere()
    {
        var instructions = new List<IInstruction>
        {
            new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(constant.AsOperand(), x.AsRegOperand()),
            new MovInstruction(y.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(constant.AsOperand(), y.AsRegOperand())
        };

        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);

        Assert.Empty(interferenceGraph[x]);
        Assert.Empty(copyGraph[x]);
    }

    [Fact]
    public void RedefinedRegistersDontInterfere()
    {
        var instructions = new List<IInstruction>
        {
            new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(constant.AsOperand(), x.AsRegOperand()),
            new MovInstruction(y.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(constant.AsOperand(), y.AsRegOperand()),
            new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(constant.AsOperand(), x.AsRegOperand())
        };

        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);

        Assert.Empty(interferenceGraph[x]);
        Assert.Empty(copyGraph[x]);
    }

    [Fact]
    public void CopiesDontInterfere()
    {
        var instructions = new List<IInstruction>
        {
            new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(y.AsRegOperand(), x.AsRegOperand()),
            new MovInstruction(constant.AsOperand(), x.AsRegOperand()),
            new MovInstruction(constant.AsOperand(), y.AsRegOperand())
        };

        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);

        Assert.Empty(interferenceGraph[x]);
        Assert.Single(copyGraph[x], y);
    }

    [Fact]
    public void ConditionalDefinitionsInterfere()
    {
        var instructions = new List<IAsmable>
        {
            new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
            new JmpCcInstruction(ConditionCode.E, conditionTarget),
            new MovInstruction(y.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(constant.AsOperand(), y.AsRegOperand()),
            conditionTarget,
            new MovInstruction(constant.AsOperand(), x.AsRegOperand())
        };

        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);

        Assert.Single(interferenceGraph[x], y);
        Assert.Empty(copyGraph[x]);
    }

    [Fact]
    public void ConditionalCopiesArentValid()
    {
        var instructions = new List<IAsmable>
        {
            new MovInstruction(y.AsRegOperand(), constant.AsOperand()),
            new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
            new JmpCcInstruction(ConditionCode.E, conditionTarget),
            new MovInstruction(y.AsRegOperand(), x.AsRegOperand()),
            conditionTarget,
            new MovInstruction(constant.AsOperand(), x.AsRegOperand()),
            new MovInstruction(constant.AsOperand(), y.AsRegOperand()),
        };

        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);

        Assert.Single(interferenceGraph[x], y);
        Assert.Empty(copyGraph[x]);
    }

#pragma warning disable xUnit1026
    [Theory]
    [MemberTupleData(nameof(InstructionLists))]
    public void GraphsAreSymmetric(List<IAsmable> instructions, object _)
    {
        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);
        foreach (var (register, conflicts) in interferenceGraph)
        {
            Assert.All(
                conflicts,
                conflicting => Assert.Contains(register, interferenceGraph[conflicting])
                );
        }

        foreach (var (register, conflicts) in copyGraph)
        {
            Assert.All(
                conflicts,
                conflicting => Assert.Contains(register, copyGraph[conflicting])
            );
        }
    }

    [Theory]
    [MemberTupleData(nameof(InstructionLists))]
    public void GraphsHaveVerticesForAllUsedRegisters(List<IAsmable> instructions, List<Register> registers)
    {
        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);
        Assert.All(registers, register => Assert.Contains(register, interferenceGraph.Keys));
        Assert.All(registers, register => Assert.Contains(register, copyGraph.Keys));
        Assert.All(interferenceGraph.Keys, register => Assert.Contains(register, registers));
        Assert.All(copyGraph.Keys, register => Assert.Contains(register, registers));
    }

    [Theory]
    [MemberTupleData(nameof(InstructionLists))]
    public void GraphsHaveNoLoops(List<IAsmable> instructions, List<Register> registers)
    {
        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);
        Assert.All(registers, register => Assert.DoesNotContain(register, interferenceGraph[register]));
        Assert.All(registers, register => Assert.DoesNotContain(register, copyGraph[register]));
    }

    [Theory]
    [MemberTupleData(nameof(InstructionLists))]
    public void InterferingPairAreNotCopies(List<IAsmable> instructions, List<Register> registers)
    {
        var (interferenceGraph, copyGraph) = LivenessAnalyzer.Process(instructions);
        Assert.All
        (
            registers,
            register =>
                Assert.All
                (
                    interferenceGraph[register],
                    conflict => Assert.DoesNotContain(conflict, copyGraph[register])
                )
        );
    }
#pragma warning restore xUnit1026

    public static readonly (List<IAsmable>, List<Register>)[] InstructionLists =
    {
        new (
            new List<IAsmable>
            {
                new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
                new MovInstruction(y.AsRegOperand(), constant.AsOperand()),
                new MovInstruction(constant.AsOperand(), x.AsRegOperand()),
                new MovInstruction(constant.AsOperand(), y.AsRegOperand())
            },
            new List<Register> { x, y }
        ),
        new (
            new List<IAsmable>
            {
                new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
                new MovInstruction(y.AsRegOperand(), constant.AsOperand()),
                new MovInstruction(constant.AsOperand(), x.AsRegOperand())
            },
            new List<Register> { x, y }
        ),
        new (
            new List<IAsmable>
            {
                new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
                new MovInstruction(y.AsRegOperand(), x.AsRegOperand()),
                new MovInstruction(constant.AsOperand(), x.AsRegOperand()),
                new MovInstruction(constant.AsOperand(), y.AsRegOperand())
            },
            new List<Register> { x, y }
        ),
        new (
            new List<IAsmable>
            {
                new MovInstruction(x.AsRegOperand(), constant.AsOperand()),
                new JmpCcInstruction(ConditionCode.E, conditionTarget),
                new MovInstruction(y.AsRegOperand(), constant.AsOperand()),
                new MovInstruction(constant.AsOperand(), y.AsRegOperand()),
                conditionTarget,
                new MovInstruction(constant.AsOperand(), x.AsRegOperand())
            },
            new List<Register> {x, y}
        )
    };
}
