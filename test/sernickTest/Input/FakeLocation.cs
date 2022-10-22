namespace sernickTest.Input;

using sernick.Input;

public class FakeLocation : ILocation
{
    public override string ToString() => base.ToString() ?? "fake";
}
