using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Demo.Stack;

public class StackDemoTests
{
    [Fact]
    public void Pop_AfterPush_ReturnsItem()
    {
        INonNullStack proxy = Dbc.Make<INonNullStack>(new NonNullStack(0));
        proxy.Push("value");

        string popped = proxy.Pop();

        Assert.Equal("value", popped);
        Assert.True(proxy.IsEmpty);
    }

    [Fact]
    public void Pop_WhenEmpty_ThrowsPreconditionViolation()
    {
        INonNullStack proxy = Dbc.Make<INonNullStack>(new NonNullStack(1));

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Pop());

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Pop", ex.Method);
    }

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property Push_RandomInputs_SatisfyContracts(string value) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<INonNullStack>(new NonNullStack(0)),
            (INonNullStack proxy) => proxy.Push(value));

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property Pop_AfterSeedPush_SatisfiesContracts(NonEmptyString value) =>
        DbcPropertyTest.Check(
            () =>
            {
                INonNullStack proxy = Dbc.Make<INonNullStack>(new NonNullStack(0));
                proxy.Push(value.Get);
                return proxy;
            },
            (INonNullStack proxy) => proxy.Pop());
}
