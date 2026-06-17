#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests;

public class DivisionProgramTests
{
    [Theory]
    [InlineData(10.0, 2.0, 5.0)]
    [InlineData(-9.0, 3.0, -3.0)]
    [InlineData(7.5, 2.5, 3.0)]
    public void Divide_ContractSatisfied(double dividend, double divisor, double expected)
    {
        IDivisionProgram proxy = Dbc.Make<IDivisionProgram>(new DivisionProgram());

        double result = proxy.Divide(dividend, divisor);

        Assert.Equal(expected, result, 12);
    }

    [Theory]
    [InlineData(1.0, 0.0)]
    [InlineData(-42.0, 0.0)]
    public void Divide_PreconditionFailed(double dividend, double divisor)
    {
        IDivisionProgram proxy = Dbc.Make<IDivisionProgram>(new DivisionProgram());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Divide(dividend, divisor));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Divide", ex.Method);
    }

    [Theory]
    [InlineData(10.0, 2.0)]
    [InlineData(9.0, 3.0)]
    public void Divide_PostconditionFailed(double dividend, double divisor)
    {
        IDivisionProgram proxy = Dbc.Make<IDivisionProgram>(new IncorrectDivisionProgram());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Divide(dividend, divisor));

        Assert.Equal(ContractKind.Postcondition, ex.Kind);
        Assert.Equal("Divide", ex.Method);
    }

    private sealed class IncorrectDivisionProgram : IDivisionProgram
    {
        public double Divide(double dividend, double divisor) => dividend;
    }
}
