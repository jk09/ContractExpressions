using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Tmp;

public class TestATests
{
    [Theory]
    [InlineData(10, 2)]
    [InlineData(-9, 3)]
    [InlineData(7, -2)]
    public void M_WithValidInput_SatisfiesContracts(int x, int y)
    {
        ITestA proxy = Dbc.Make<ITestA>(new TestA());

        int result = proxy.M(x, y);

        Assert.Equal(x / y, result);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(-5, 0)]
    public void M_WhenPreconditionFails_ThrowsPreconditionViolation(int x, int y)
    {
        ITestA proxy = Dbc.Make<ITestA>(new TestA());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.M(x, y));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("M", ex.Method);
    }

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property M_RandomInputs_SatisfiesContracts(int x, int y) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<ITestA>(new TestA()),
            (ITestA proxy) => proxy.M(x, y));
}
