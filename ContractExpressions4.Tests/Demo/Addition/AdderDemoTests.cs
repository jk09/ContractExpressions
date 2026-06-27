using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Demo.Addition;

public class AdderDemoTests
{
    [Fact]
    public void Add_TwoPositiveNumbers_ReturnsSum()
    {
        IAdder proxy = Dbc.Make<IAdder>(new Adder());

        int result = proxy.Add(3, 4);

        Assert.Equal(7, result);
    }

    [Fact]
    public void Add_NegativeAndPositive_ReturnsSum()
    {
        IAdder proxy = Dbc.Make<IAdder>(new Adder());

        int result = proxy.Add(-5, 10);

        Assert.Equal(5, result);
    }

    [Fact]
    public void Add_Zeros_ReturnsZero()
    {
        IAdder proxy = Dbc.Make<IAdder>(new Adder());

        int result = proxy.Add(0, 0);

        Assert.Equal(0, result);
    }

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property Add_AnyIntegers_SatisfyContracts(int a, int b) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IAdder>(new Adder()),
            (IAdder proxy) => proxy.Add(a, b));
}
