using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Demo.NonNullStackWithUpdate;

public class NonNullStackWithUpdateDemoTests
{
    [Fact]
    public void UpdateAt_WithValidIndex_UpdatesValue()
    {
        INonNullStackWithUpdates proxy = Dbc.Make<INonNullStackWithUpdates>(new NonNullStackWithUpdates(1));
        proxy.Push("a");

        proxy.UpdateAt(0, "b");

        string current = proxy.Pop();
        Assert.Equal("b", current);
    }

    [Fact]
    public void UpdateAt_WhenIndexOutOfRange_ThrowsPreconditionViolation()
    {
        INonNullStackWithUpdates proxy = Dbc.Make<INonNullStackWithUpdates>(new NonNullStackWithUpdates(1));
        proxy.Push("a");

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.UpdateAt(1, "b"));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("UpdateAt", ex.Method);
    }

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property UpdateAt_RandomInputs_SatisfyContracts(NonEmptyString value) =>
        DbcPropertyTest.Check(
            () =>
            {
                INonNullStackWithUpdates proxy = Dbc.Make<INonNullStackWithUpdates>(new NonNullStackWithUpdates(1));
                proxy.Push("seed");
                return proxy;
            },
            (INonNullStackWithUpdates proxy) => proxy.UpdateAt(0, value.Get));
}
