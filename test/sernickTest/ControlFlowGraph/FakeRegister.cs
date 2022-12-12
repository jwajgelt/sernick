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
    private FakeHardwareRegister(string label) : base(label)
    {
    }
    public bool Equals(FakeHardwareRegister? other) => other != null && _label == other._label;

    public override bool Equals(object? obj) => obj is FakeHardwareRegister other && Equals(other);

    public override int GetHashCode() => _label.GetHashCode();

    public static implicit operator FakeHardwareRegister(string id) => new(id);
}

