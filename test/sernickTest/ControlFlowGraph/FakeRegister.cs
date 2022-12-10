namespace sernickTest.ControlFlowGraph;

using sernick.ControlFlowGraph.CodeTree;

public class FakeRegister : Register, IEquatable<FakeRegister>
{
    private readonly int _id;

    public FakeRegister(int id)
    {
        _id = id;
    }

    public bool Equals(FakeRegister? other) => other != null && _id == other._id;

    public override bool Equals(object? obj) => obj is FakeRegister other && Equals(other);

    public override int GetHashCode() => _id;

    public static implicit operator FakeRegister(int id) => new(id);
}

public class FakeHardwareRegister : HardwareRegister, IEquatable<FakeHardwareRegister>
{
    private readonly char _id;

    private FakeHardwareRegister(char id)
    {
        _id = id;
    }
    public bool Equals(FakeHardwareRegister? other) => other != null && _id == other._id;

    public override bool Equals(object? obj) => obj is FakeHardwareRegister other && Equals(other);

    public override int GetHashCode() => _id;

    public static implicit operator FakeHardwareRegister(char id) => new(id);
}

