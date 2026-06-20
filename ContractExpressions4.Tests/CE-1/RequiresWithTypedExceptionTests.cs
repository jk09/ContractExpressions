#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.CE1;

/// <summary>
/// Tests for Contract.Requires&lt;TException&gt; with typed exceptions beyond ArgumentNullException.
/// Inspired by CodeContracts/Demo/Devscovery/MyMath.cs Abs_NetStyle.
/// Covers: Requires&lt;OverflowException&gt;, Ensures with Result&lt;int&gt; non-negative.
/// </summary>
public class RequiresWithTypedExceptionTests : IClassFixture<ContractFailureUnwindFixture>
{
    [Fact]
    public void Abs_PositiveValue_ReturnsValue()
    {
        var proxy = Dbc.Make<IMath>(new SimpleMath());
        Assert.Equal(42, proxy.Abs(42));
    }

    [Fact]
    public void Abs_NegativeValue_ReturnsAbsoluteValue()
    {
        var proxy = Dbc.Make<IMath>(new SimpleMath());
        Assert.Equal(7, proxy.Abs(-7));
    }

    [Fact]
    public void Abs_Zero_ReturnsZero()
    {
        var proxy = Dbc.Make<IMath>(new SimpleMath());
        Assert.Equal(0, proxy.Abs(0));
    }

    [Fact]
    public void Abs_MinValue_ThrowsOverflowException()
    {
        var proxy = Dbc.Make<IMath>(new SimpleMath());
        Assert.Throws<OverflowException>(() => proxy.Abs(int.MinValue));
    }
}

[ContractClass(typeof(MathContracts))]
interface IMath
{
    int Abs(int x);
}

class SimpleMath : IMath
{
    public int Abs(int x) => Math.Abs(x);
}

[ContractClassFor(typeof(IMath))]
class MathContracts
{
    public MathContracts()
    {
        Dbc.Def(static (IMath m, int x) => m.Abs(x),
                static (IMath m, int x) => Contract.Requires<OverflowException>(x > int.MinValue),
                static (IMath m, int x) => Contract.Ensures(Contract.Result<int>() >= 0));
    }
}
