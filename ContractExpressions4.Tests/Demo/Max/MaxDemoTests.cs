using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Demo.Max;

public class MaxDemoTests
{
    [Fact]
    public void Max_WithValidInput_ReturnsMaximum()
    {
        IMaxUtils proxy = Dbc.Make<IMaxUtils>(new MaxUtils());

        int result = proxy.Max([3, -2, 9, 1]);

        Assert.Equal(9, result);
    }

    [Fact]
    public void Max_WhenInputIsNull_ThrowsPreconditionViolation()
    {
        IMaxUtils proxy = Dbc.Make<IMaxUtils>(new MaxUtils());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Max(null!));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Max", ex.Method);
    }

    [Fact]
    public void Max_WhenInputIsEmpty_ThrowsPostconditionViolation()
    {
        IMaxUtils proxy = Dbc.Make<IMaxUtils>(new MaxUtils());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Max([]));

        Assert.Equal(ContractKind.Postcondition, ex.Kind);
        Assert.Equal("Max", ex.Method);
    }

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property Max_NonEmptyArrays_SatisfyContracts(NonEmptyArray<int> elements) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IMaxUtils>(new MaxUtils()),
            (IMaxUtils proxy) => proxy.Max(elements.Get));
}
