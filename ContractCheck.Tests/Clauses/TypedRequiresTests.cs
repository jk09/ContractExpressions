#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Clauses.Requires;

/// <summary>
/// <c>Contract.Requires&lt;TException&gt;</c> throws <c>TException</c>, constructed like the CodeContracts rewriter did
/// (resolution R3): a <c>(paramName, message)</c> constructor gets the user message as the parameter name.
/// ContractExpressions4 ignored <c>TException</c>; ContractExpressions dropped the condition and the parameter name.
/// </summary>
public class TypedRequiresTests
{
    [Fact]
    public void ArgumentNullException_is_thrown_ported_from_PreconditionOverloadTests()
    {
        IChecked proxy = Dbc.Make<IChecked>(new Checked());

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => proxy.Store(null!));

        Assert.Equal(ContractFailureKind.Precondition, exception.GetContractFailureKind());
        Assert.True(exception.IsEntryCheckFailure());
    }

    [Fact]
    public void ArgumentNullException_with_message_contains_the_message_ported_from_PreconditionOverloadTests()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Dbc.Make<IChecked>(new Checked()).StoreWithMessage(null!));

        Assert.Contains("Object cannot be null", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ArgumentNullException_gets_the_user_message_as_ParamName_ported_from_NegativeRequiresExceptionTest1()
    {
        // CodeContracts Foxtrot/Tests/RewrittenContractTest.cs: Requires<ArgumentNullException>(parameter != null, "parameter").
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Dbc.Make<IChecked>(new Checked()).Named(null!));

        Assert.Equal("parameter", exception.ParamName);
        Assert.StartsWith("Precondition failed: parameter != null  parameter", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ArgumentOutOfRangeException_gets_the_user_message_as_ParamName_ported_from_NegativeRequiresExceptionTest2()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Dbc.Make<IChecked>(new Checked()).NonNegative(-1));

        Assert.Equal("parameter", exception.ParamName);
    }

    [Fact]
    public void ArgumentException_gets_the_failure_message_ported_from_NegativeRequiresExceptionTest3()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Dbc.Make<IChecked>(new Checked()).Ordered(0, 1));

        Assert.Null(exception.ParamName);
        Assert.Equal("Precondition failed: x >= y  x must be at least y", exception.Message);
    }

    [Theory]
    [InlineData(42, 42)]
    [InlineData(-7, 7)]
    [InlineData(0, 0)]
    public void Valid_inputs_pass_the_typed_precondition_ported_from_RequiresWithTypedExceptionTests(int x, int expected)
    {
        // ContractExpressions.Tests/CE-1/RequiresWithTypedExceptionTests.cs: Abs_PositiveValue / Abs_NegativeValue / Abs_Zero.
        Assert.Equal(expected, Dbc.Make<IChecked>(new Checked()).Abs(x));
    }

    [Fact]
    public void OverflowException_is_thrown_ported_from_RequiresWithTypedExceptionTests()
    {
        // ContractExpressions.Tests/CE-1/RequiresWithTypedExceptionTests.cs: Abs_MinValue (failed in ContractExpressions4).
        OverflowException exception = Assert.Throws<OverflowException>(() => Dbc.Make<IChecked>(new Checked()).Abs(int.MinValue));

        Assert.Equal("Precondition failed: x > -2147483648", exception.Message);
    }

    [Fact]
    public void Exception_with_only_a_parameterless_constructor_is_created_with_it()
    {
        Assert.Throws<SilentFailure>(() => Dbc.Make<IChecked>(new Checked()).Silent(false));
    }

    [Fact]
    public void Typed_precondition_records_the_condition_in_Data()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => Dbc.Make<IChecked>(new Checked()).Started(false));

        Assert.Equal("started", exception.Data["ContractCheck.Condition"]);
        Assert.Equal(ContractFailureKind.Precondition, exception.Data["ContractCheck.FailureKind"]);
        Assert.Equal(ContractFailureKind.Precondition, exception.GetContractFailureKind());
        Assert.True(exception.IsEntryCheckFailure());
    }
}

[ContractClass(typeof(CheckedContracts))]
public interface IChecked
{
    void Store(object item);

    void StoreWithMessage(object item);

    void Named(string parameter);

    void NonNegative(int parameter);

    void Ordered(int x, int y);

    int Abs(int x);

    void Silent(bool ok);

    void Started(bool started);
}

public sealed class Checked : IChecked
{
    public void Store(object item)
    {
    }

    public void StoreWithMessage(object item)
    {
    }

    public void Named(string parameter)
    {
    }

    public void NonNegative(int parameter)
    {
    }

    public void Ordered(int x, int y)
    {
    }

    public int Abs(int x) => Math.Abs(x);

    public void Silent(bool ok)
    {
    }

    public void Started(bool started)
    {
    }
}

public sealed class SilentFailure : Exception
{
}

[ContractClassFor(typeof(IChecked))]
internal sealed class CheckedContracts
{
    public CheckedContracts()
    {
        Dbc.Def(static (IChecked x, object item) => x.Store(item),
            static (IChecked x, object item) => Contract.Requires<ArgumentNullException>(item != null));

        Dbc.Def(static (IChecked x, object item) => x.StoreWithMessage(item),
            static (IChecked x, object item) => Contract.Requires<ArgumentNullException>(item != null, "Object cannot be null"));

        Dbc.Def(static (IChecked x, string parameter) => x.Named(parameter),
            static (IChecked x, string parameter) => Contract.Requires<ArgumentNullException>(parameter != null, "parameter"));

        Dbc.Def(static (IChecked x, int parameter) => x.NonNegative(parameter),
            static (IChecked x, int parameter) => Contract.Requires<ArgumentOutOfRangeException>(parameter >= 0, "parameter"));

        Dbc.Def(static (IChecked c, int x, int y) => c.Ordered(x, y),
            static (IChecked c, int x, int y) => Contract.Requires<ArgumentException>(x >= y, "x must be at least y"));

        Dbc.Def(static (IChecked m, int x) => m.Abs(x),
            static (IChecked m, int x) => Contract.Requires<OverflowException>(x > int.MinValue),
            static (IChecked m, int x) => Contract.Ensures(Contract.Result<int>() >= 0));

        Dbc.Def(static (IChecked x, bool ok) => x.Silent(ok),
            static (IChecked x, bool ok) => Contract.Requires<SilentFailure>(ok));

        Dbc.Def(static (IChecked x, bool started) => x.Started(started),
            static (IChecked x, bool started) => Contract.Requires<InvalidOperationException>(started));
    }
}
