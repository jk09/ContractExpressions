#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions;

namespace ContractExpressions.Tests.CE1;

/// <summary>
/// Tests inspired by CodeContracts/Demo/Operations/IOperation.cs.
/// Covers: Requires on division, Ensures with exact result assertion,
/// Result on int return type, multiple parameter contracts.
/// </summary>
public class OperationsContractTests : IClassFixture<ContractFailureUnwindFixture>
{
    private readonly ICalculator _proxy;

    public OperationsContractTests()
    {
        _proxy = Dbc.Make<ICalculator>(new Calculator());
    }

    [Fact]
    public void Add_HappyPath_ReturnsSum()
    {
        Assert.Equal(7, _proxy.Add(3, 4));
    }

    [Fact]
    public void Add_NegativeNumbers_ReturnsSum()
    {
        Assert.Equal(-3, _proxy.Add(-1, -2));
    }

    [Fact]
    public void Add_Zero_ReturnsOther()
    {
        Assert.Equal(5, _proxy.Add(5, 0));
    }

    [Fact]
    public void Divide_HappyPath_ReturnsQuotient()
    {
        Assert.Equal(5, _proxy.Divide(10, 2));
    }

    [Fact]
    public void Divide_ByZero_ThrowsPreconditionFailure()
    {
        var ex = Assert.ThrowsAny<Exception>(() => _proxy.Divide(10, 0));
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
    }

    [Fact]
    public void Divide_NegativeDivisor_Succeeds()
    {
        Assert.Equal(-5, _proxy.Divide(10, -2));
    }
}

[ContractClass(typeof(CalculatorContracts))]
interface ICalculator
{
    int Add(int a, int b);
    int Divide(int a, int divisor);
}

class Calculator : ICalculator
{
    public int Add(int a, int b) => a + b;
    public int Divide(int a, int divisor) => a / divisor;
}

[ContractClassFor(typeof(ICalculator))]
class CalculatorContracts
{
    public CalculatorContracts()
    {
        // Add: postcondition asserts exact result value
        Dbc.Def(static (ICalculator x, int a, int b) => x.Add(a, b),
                static (ICalculator x, int a, int b) => Contract.Ensures(Contract.Result<int>() == a + b));

        // Divide: divisor must not be zero
        Dbc.Def(static (ICalculator x, int a, int divisor) => x.Divide(a, divisor),
                static (ICalculator x, int a, int divisor) => Contract.Requires(divisor != 0));
    }
}
