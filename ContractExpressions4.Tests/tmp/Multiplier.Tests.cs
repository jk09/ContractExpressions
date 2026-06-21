using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests.Tmp;

public class MultiplierTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(12, 7)]
    public void Multiply_WithValidInput_SatisfiesContracts(int a, int b)
    {
        IMultiplier proxy = Dbc.Make<IMultiplier>(new Multiplier());

        int result = proxy.Multiply(a, b);

        Assert.Equal(a * b, result);
        Assert.True(result > 0);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 2)]
    [InlineData(2, -1)]
    public void Multiply_WhenPreconditionFails_ThrowsPreconditionViolation(int a, int b)
    {
        IMultiplier proxy = Dbc.Make<IMultiplier>(new Multiplier());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Multiply(a, b));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Multiply", ex.Method);
    }

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property Multiply_PositiveSmallInputs_SatisfiesContracts(byte a, byte b)
    {
        // Keep generated values in a non-overflowing positive range for deterministic postcondition checks.
        int x = a + 1;
        int y = b + 1;

        return DbcPropertyTest.Check(
            () => Dbc.Make<IMultiplier>(new Multiplier()),
            (IMultiplier proxy) => proxy.Multiply(x, y));
    }
}
