using sernick.Input;

namespace sernickTest.Input;

public class FakeLocation : ILocation
{
    public override string ToString() => base.ToString() ?? "fake";
}
