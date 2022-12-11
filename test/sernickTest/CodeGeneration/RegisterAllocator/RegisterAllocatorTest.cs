namespace sernickTest.CodeGeneration.RegisterAllocator;

using ControlFlowGraph;
using Helpers;
using sernick.CodeGeneration.RegisterAllocation;
using sernick.ControlFlowGraph.CodeTree;
using Utility;
using static Helpers.GraphBuilder;
using Graph = IReadOnlyDictionary<sernick.ControlFlowGraph.CodeTree.Register, IReadOnlyCollection<sernick.ControlFlowGraph.CodeTree.Register>>;
using Allocation = IReadOnlyDictionary<sernick.ControlFlowGraph.CodeTree.Register, sernick.ControlFlowGraph.CodeTree.HardwareRegister?>;

public class RegisterAllocatorTest
{
    [Fact]
    public void Allocates_All_Registers()
    {
        var hardwareRegisters = new FakeHardwareRegister[] { "A", "B", "C" };
        var interferenceGraph = Graph()
            .Edge(1, 2)
            .Edge(2, 3)
            .Edge(3, 1)
            .Build();
        var copyGraph = Graph().Build();

        var allocator = new RegisterAllocator(hardwareRegisters);
        var allocated = allocator.Process(interferenceGraph, copyGraph);

        AssertValidAllocation(allocated, interferenceGraph);
        AssertAllocatedAtLeast(allocated, 3);
        AssertUsesOnlySpecifiedHardwareRegisters(allocated, hardwareRegisters);
    }

    [Fact]
    public void Allocates_Null_When_Impossible()
    {
        var hardwareRegisters = new FakeHardwareRegister[] { "A" };
        var interferenceGraph = Graph()
            .Edge(1, 2)
            .Edge(2, 3)
            .Edge(3, 1)
            .Build();
        var copyGraph = Graph().Build();

        var allocator = new RegisterAllocator(hardwareRegisters);
        var allocated = allocator.Process(interferenceGraph, copyGraph);

        AssertValidAllocation(allocated, interferenceGraph);
        AssertAllocatedAtLeast(allocated, 1);
        AssertUsesOnlySpecifiedHardwareRegisters(allocated, hardwareRegisters);
    }

    [Fact]
    public void Connects_Registers()
    {
        var hardwareRegisters = new FakeHardwareRegister[] { "A", "B", "C" };
        var interferenceGraph = Graph()
            .Edge(1, 2)
            .Edge(2, 3)
            .Edge(3, 1)
            .Edge(2, 4)
            .Edge(4, 1)
            .Build();
        var copyGraph = Graph().Edge(3, 4).Build();

        var allocator = new RegisterAllocator(hardwareRegisters);
        var allocated = allocator.Process(interferenceGraph, copyGraph);

        AssertValidAllocation(allocated, interferenceGraph);
        AssertAllocatedAtLeast(allocated, 3);
        AssertUsesOnlySpecifiedHardwareRegisters(allocated, hardwareRegisters);

        Assert.Equal(allocated[(FakeRegister)3], allocated[(FakeRegister)4]);
    }

    [Fact]
    public void Allocates_Same_HardwareRegisters()
    {
        var hardwareRegisters = new FakeHardwareRegister[] { "A", "B", "C" };
        var interferenceGraph = Graph()
            .Edge(2, "A")
            .Edge(3, "A")
            .Edge(2, 3)
            .Build();
        var copyGraph = Graph().Build();

        var allocator = new RegisterAllocator(hardwareRegisters);
        var allocated = allocator.Process(interferenceGraph, copyGraph);

        AssertValidAllocation(allocated, interferenceGraph);
        AssertAllocatedAtLeast(allocated, 3);
        AssertPreservesHardwareRegisters(allocated);
        AssertUsesOnlySpecifiedHardwareRegisters(allocated, hardwareRegisters);
    }

    [Theory]
    [MemberTupleData(nameof(ComplexCasesTestData))]
    public void Allocation_Handles_ComplexCases(FakeHardwareRegister[] hardwareRegisters, Graph interferenceGraph,
        Graph copyGraph, int minAllocated)
    {
        var allocator = new RegisterAllocator(hardwareRegisters);
        var allocated = allocator.Process(interferenceGraph, copyGraph);

        AssertValidAllocation(allocated, interferenceGraph);
        AssertAllocatedAtLeast(allocated, minAllocated);
        AssertPreservesHardwareRegisters(allocated);
        AssertUsesOnlySpecifiedHardwareRegisters(allocated, hardwareRegisters);
    }

    public static readonly (FakeHardwareRegister[] hardwareRegisters, Graph interferenceGraph, Graph copyGraph, int
        minAllocated)[] ComplexCasesTestData =
        {
            (
                hardwareRegisters: new FakeHardwareRegister[]{ "A", "B", "C"},
                interferenceGraph: Graph()
                    .Edge(1, 2)
                    .Edge(1, 3)
                    .Edge(1, 4)
                    .Edge(2, 3)
                    .Edge(2, 5)
                    .Edge(4, 6)
                    .Edge(4, "A")
                    .Edge(5, 6)
                    .Edge(5, "A")
                    .Edge(6, "A")
                    .Build(),
                copyGraph: Graph()
                    .Edge(2, 6)
                    .Edge(3, 6)
                    .Edge(4, 5)
                    .Edge(4, 2)
                    .Build(),
                minAllocated: 3),
            (
                hardwareRegisters: new FakeHardwareRegister[]{ "A", "B", "C"},
                interferenceGraph: Graph()
                    .Edge(1, 2)
                    .Edge(1, "A")
                    .Edge(1, "B")
                    .Edge(2, 3)
                    .Edge(3, 4)
                    .Edge(3, "B")
                    .Edge(4, "A")
                    .Edge(4, "B")
                    .Edge("A", "B")
                    .Build(),
                copyGraph: Graph()
                    .Edge(1, 3)
                    .Edge(2, 4)
                    .Build(),
                minAllocated: 4),
            (
                hardwareRegisters: new FakeHardwareRegister[]{ "A", "B"},
                interferenceGraph: Graph()
                    .Edge(1, 2)
                    .Edge(2, 3)
                    .Edge(3, 4)
                    .Edge(4, 5)
                    .Edge(5, 6)
                    .Edge(6, "A")
                    .Edge(6, "B")
                    .Edge(1, 4)
                    .Edge(2, 6)
                    .Edge(3, "A")
                    .Edge(5, "B")
                    .Build(),
                copyGraph: Graph()
                    .Edge(1, 5)
                    .Edge(3, "B")
                    .Build(),
                minAllocated: 5),
            (
                hardwareRegisters: new FakeHardwareRegister[]{ "A", "B", "C"},
                interferenceGraph: Graph()
                    .Edge(1, 2)
                    .Edge(1, 3)
                    .Edge(1, 4)
                    .Edge(2, 6)
                    .Edge(2, 9)
                    .Edge(3, 5)
                    .Edge(3, 8)
                    .Edge(4, 7)
                    .Edge(4, 10)
                    .Edge(5, 6)
                    .Edge(6, 7)
                    .Edge(7, 8)
                    .Edge(8, 9)
                    .Edge(9, 10)
                    .Build(),
                copyGraph: Graph()
                    .Edge(6, 8)
                    .Edge(8, 10)
                    .Edge(10, 6)
                    .Build(),
                minAllocated: 10),

            // Large special graphs
            (
                hardwareRegisters: new FakeHardwareRegister[]{ "A", "B", "C", "D" },
                interferenceGraph: Clique(64),
                copyGraph: Graph().Build(),
                minAllocated: 4),
            (
                hardwareRegisters: new FakeHardwareRegister[]{ "A", "B"},
                interferenceGraph: StarGraph(64),
                copyGraph: Graph().Build(),
                minAllocated: 65),

            // Small Edge Cases
            (
                hardwareRegisters: new FakeHardwareRegister[]{ "A" },
                interferenceGraph: Graph().Build(),
                copyGraph: Graph().Build(),
                minAllocated: 0),
            (
                hardwareRegisters: new FakeHardwareRegister[]{ "A", "B" },
                interferenceGraph: Graph().Edge(1, 2).Edge(3, 4).Edge(5, 6).Build(),
                copyGraph: Graph().Build(),
                minAllocated: 6),
        };

    /// <summary>
    /// Checks if every register pair connected by an edge has different allocated hardware register.
    /// </summary>
    private static void AssertValidAllocation(Allocation allocation, Graph interferenceGraph)
    {
        Assert.All(interferenceGraph.Edges(), edge =>
        {
            var fromRegister = allocation.GetValueOrDefault(edge.from);
            var toRegister = allocation.GetValueOrDefault(edge.to);
            Assert.True(fromRegister == null || toRegister == null || fromRegister != toRegister,
                $"Allocated ${(fromRegister, toRegister)} to edge ${edge}");
        });
    }

    private static void AssertAllocatedAtLeast(Allocation allocation, int min)
    {
        Assert.InRange(allocation.Count(kv => kv.Value != null), min, allocation.Count);
    }

    private static void AssertPreservesHardwareRegisters(Allocation allocation)
    {
        Assert.All(allocation, (kv) =>
        {
            if (kv.Key is HardwareRegister)
            {
                Assert.Equal(kv.Key, kv.Value);
            }
        });
    }

    private static void AssertUsesOnlySpecifiedHardwareRegisters(Allocation allocation,
        ICollection<HardwareRegister> hardwareRegisters)
    {
        var nonNullAllocation = allocation.Where((kv) => kv.Value != null);
        Assert.All(nonNullAllocation, (kv) => 
        {
            Assert.Contains(kv.Value, hardwareRegisters);
        });
    }
}
