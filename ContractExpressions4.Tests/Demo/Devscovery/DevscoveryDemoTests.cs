using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Demo.Devscovery;

public class DevscoveryDemoTests
{
    [Fact]
    public void AbsNetStyle_WhenMinValue_ThrowsPreconditionViolation()
    {
        IMyMath proxy = Dbc.Make<IMyMath>(new MyMathService());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.AbsNetStyle(int.MinValue));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("AbsNetStyle", ex.Method);
    }

    [Fact]
    public void AbsJavaStyle_WhenMinValue_SatisfiesContract()
    {
        IMyMath proxy = Dbc.Make<IMyMath>(new MyMathService());

        int result = proxy.AbsJavaStyle(int.MinValue);

        Assert.Equal(int.MinValue, result);
    }

    [Fact]
    public void ArrayAbs_WhenInputIsNull_ThrowsPreconditionViolation()
    {
        IArrayExtensionsDemo proxy = Dbc.Make<IArrayExtensionsDemo>(new ArrayExtensionsDemo());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Abs(null!));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Abs", ex.Method);
    }

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property AbsNetStyle_RandomInputs_SatisfyContracts(int value) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IMyMath>(new MyMathService()),
            (IMyMath proxy) => proxy.AbsNetStyle(value));

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property ArrayAbs_RandomInputs_SatisfyContracts(int[] values) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<IArrayExtensionsDemo>(new ArrayExtensionsDemo()),
            (IArrayExtensionsDemo proxy) => proxy.Abs(values));
}
