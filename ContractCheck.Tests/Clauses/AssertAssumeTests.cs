#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Clauses.AssertAssume;

/// <summary>
/// <c>Contract.Assert</c> and <c>Contract.Assume</c> (resolution R10): checked when the member is entered, together with the
/// preconditions and in definition order, and reported with their own failure kinds.
/// Ported from ContractExpressions(4).Tests/CE-1/AssertAndAssumeTests.cs (which failed in ContractExpressions4) and the
/// Assert/Assume cases of CodeContracts Foxtrot/Tests/RewrittenContractTest.cs.
/// </summary>
public class AssertAssumeTests
{
    [Fact]
    public void Assert_condition_true_passes()
    {
        Assert.True(Dbc.Make<IValidator>(new Validator()).ValidatePositive(5));
    }

    [Fact]
    public void Assert_condition_false_throws()
    {
        Expect.Violation(ContractFailureKind.Assert, () => Dbc.Make<IValidator>(new Validator()).ValidatePositive(-1));
    }

    [Fact]
    public void Assert_message_appears_in_the_exception()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Assert, () => Dbc.Make<IValidator>(new Validator()).ValidatePositiveWithMessage(-1));

        Assert.Contains("Value must be positive", exception.Message, StringComparison.Ordinal);
        Assert.StartsWith("Assertion failed: value > 0", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Assume_condition_true_passes()
    {
        Assert.True(Dbc.Make<IValidator>(new Validator()).ValidateBelowLimit(500));
    }

    [Fact]
    public void Assume_condition_false_throws()
    {
        Expect.Violation(ContractFailureKind.Assume, () => Dbc.Make<IValidator>(new Validator()).ValidateBelowLimit(1001));
    }

    [Fact]
    public void Assume_message_appears_in_the_exception()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Assume, () => Dbc.Make<IValidator>(new Validator()).ValidateBelowLimitWithMessage(1001));

        Assert.Contains("Value assumed under limit", exception.Message, StringComparison.Ordinal);
        Assert.StartsWith("Assumption failed: value <= 1000", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Assert_runs_before_the_member_body()
    {
        var target = new Validator();

        Expect.Violation(ContractFailureKind.Assert, () => Dbc.Make<IValidator>(target).ValidatePositive(-1));

        Assert.Equal(0, target.Calls);
    }

    [Fact]
    public void Entry_checks_run_in_definition_order()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Assume, () => Dbc.Make<IValidator>(new Validator()).Mixed(-1));

        Assert.Equal("value != -1", exception.Condition);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Assert_and_Assume_of_an_argument_ported_from_RewrittenContractTest(bool mustBeTrue)
    {
        IValidator proxy = Dbc.Make<IValidator>(new Validator());

        if (mustBeTrue)
        {
            proxy.AssertsTrue(true);
            proxy.AssumesTrue(true);
        }
        else
        {
            Expect.Violation(ContractFailureKind.Assert, () => proxy.AssertsTrue(false));
            Expect.Violation(ContractFailureKind.Assume, () => proxy.AssumesTrue(false));
        }
    }
}

[ContractClass(typeof(ValidatorContracts))]
public interface IValidator
{
    bool ValidatePositive(int value);

    bool ValidatePositiveWithMessage(int value);

    bool ValidateBelowLimit(int value);

    bool ValidateBelowLimitWithMessage(int value);

    void Mixed(int value);

    void AssertsTrue(bool mustBeTrue);

    void AssumesTrue(bool mustBeTrue);
}

public sealed class Validator : IValidator
{
    public int Calls { get; private set; }

    public bool ValidatePositive(int value) => ++Calls > 0 && value > 0;

    public bool ValidatePositiveWithMessage(int value) => value > 0;

    public bool ValidateBelowLimit(int value) => value <= 1000;

    public bool ValidateBelowLimitWithMessage(int value) => value <= 1000;

    public void Mixed(int value)
    {
    }

    public void AssertsTrue(bool mustBeTrue)
    {
    }

    public void AssumesTrue(bool mustBeTrue)
    {
    }
}

[ContractClassFor(typeof(IValidator))]
internal sealed class ValidatorContracts
{
    public ValidatorContracts()
    {
        Dbc.Def(static (IValidator x, int value) => x.ValidatePositive(value),
            static (IValidator x, int value) => Contract.Assert(value > 0));

        Dbc.Def(static (IValidator x, int value) => x.ValidatePositiveWithMessage(value),
            static (IValidator x, int value) => Contract.Assert(value > 0, "Value must be positive"));

        Dbc.Def(static (IValidator x, int value) => x.ValidateBelowLimit(value),
            static (IValidator x, int value) => Contract.Assume(value <= 1000));

        Dbc.Def(static (IValidator x, int value) => x.ValidateBelowLimitWithMessage(value),
            static (IValidator x, int value) => Contract.Assume(value <= 1000, "Value assumed under limit"));

        Dbc.Def(static (IValidator x, int value) => x.Mixed(value),
            static (IValidator x, int value) => Contract.Requires(value > -10),
            static (IValidator x, int value) => Contract.Assume(value != -1),
            static (IValidator x, int value) => Contract.Assert(value >= 0));

        Dbc.Def(static (IValidator x, bool mustBeTrue) => x.AssertsTrue(mustBeTrue),
            static (IValidator x, bool mustBeTrue) => Contract.Assert(mustBeTrue));

        Dbc.Def(static (IValidator x, bool mustBeTrue) => x.AssumesTrue(mustBeTrue),
            static (IValidator x, bool mustBeTrue) => Contract.Assume(mustBeTrue));
    }
}
