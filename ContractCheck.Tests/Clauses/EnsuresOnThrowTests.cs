#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Clauses.EnsuresOnThrow;

/// <summary>
/// <c>Contract.EnsuresOnThrow&lt;TException&gt;</c> (resolution R11): checked only when the member throws <c>TException</c>
/// or a subclass – every matching clause, in definition order. A failure wraps the thrown exception; otherwise the original
/// exception propagates unchanged. ContractExpressions evaluated the clause for any exception; ContractExpressions4 rejected it.
/// </summary>
public class EnsuresOnThrowTests
{
    [Fact]
    public void Valid_input_succeeds_ported_from_EnsuresOnThrowTests()
    {
        Assert.Equal(42, Dbc.Make<IParser>(new Parser()).Parse("42"));
    }

    [Fact]
    public void Matching_exception_with_a_holding_condition_propagates_ported_from_EnsuresOnThrowTests()
    {
        Assert.Throws<FormatException>(() => Dbc.Make<IParser>(new Parser()).Parse("not_a_number"));
    }

    [Fact]
    public void Exception_of_another_type_does_not_trigger_the_clause()
    {
        // ContractExpressions.Tests expected a contract failure here, but int.Parse(null) throws ArgumentNullException,
        // not FormatException, so EnsuresOnThrow<FormatException> does not apply (legacy semantics, probe S14).
        Assert.Throws<ArgumentNullException>(() => Dbc.Make<IParser>(new Parser()).Parse(null!));
    }

    [Fact]
    public void Matching_exception_with_a_failing_condition_is_a_contract_failure_ported_from_EnsuresOnThrowTests()
    {
        ContractViolationException exception = Expect.Violation(
            ContractFailureKind.PostconditionOnException,
            () => Dbc.Make<IParser>(new Parser()).ParseNullAsFormatError(null!));

        Assert.Contains("input was null on throw", exception.Message, StringComparison.Ordinal);
        Assert.StartsWith("Postcondition failed after throwing an exception: input != null", exception.Message, StringComparison.Ordinal);
        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Theory]
    [InlineData(0, false, null)]
    [InlineData(1, true, typeof(ApplicationException))]
    [InlineData(2, true, typeof(SystemException))]
    [InlineData(3, true, typeof(Exception))]
    public void Positive_throw_cases_ported_from_RewrittenContractTest_ThrowsEnsuresTrue(int exception, bool trueIfThrow, Type? expected)
    {
        IThrowing proxy = Dbc.Make<IThrowing>(new Throwing());

        Exception? thrown = Record.Exception(() => proxy.ThrowsEnsuresTrue(exception, trueIfThrow));

        Assert.Equal(expected, thrown?.GetType());
    }

    [Theory]
    [InlineData(0, true, ContractFailureKind.Postcondition)]
    [InlineData(1, false, ContractFailureKind.PostconditionOnException)]
    [InlineData(2, false, ContractFailureKind.PostconditionOnException)]
    [InlineData(3, false, ContractFailureKind.PostconditionOnException)]
    public void Negative_throw_cases_ported_from_RewrittenContractTest_ThrowsEnsuresTrue(int exception, bool trueIfThrow, ContractFailureKind kind)
    {
        Expect.Violation(kind, () => Dbc.Make<IThrowing>(new Throwing()).ThrowsEnsuresTrue(exception, trueIfThrow));
    }

    [Fact]
    public void Every_matching_clause_is_checked_not_only_the_first()
    {
        ContractViolationException exception = Expect.Violation(
            ContractFailureKind.PostconditionOnException,
            () => Dbc.Make<IThrowing>(new Throwing()).ThrowsInvalidOperation());

        Assert.Equal("specific", exception.UserMessage);
    }

    [Fact]
    public void Old_values_describe_what_a_failing_member_must_leave_unchanged()
    {
        IThrowing proxy = Dbc.Make<IThrowing>(new Throwing());

        Assert.Throws<InvalidOperationException>(() => proxy.Transfer(partial: false));
        Expect.Violation(ContractFailureKind.PostconditionOnException, () => proxy.Transfer(partial: true));
    }
}

[ContractClass(typeof(ParserContracts))]
public interface IParser
{
    int Parse(string input);

    int ParseNullAsFormatError(string input);
}

public sealed class Parser : IParser
{
    public int Parse(string input) => int.Parse(input, System.Globalization.CultureInfo.InvariantCulture);

    public int ParseNullAsFormatError(string input) =>
        input is null ? throw new FormatException("no input") : int.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
}

[ContractClassFor(typeof(IParser))]
internal sealed class ParserContracts
{
    public ParserContracts()
    {
        Dbc.Def(static (IParser x, string input) => x.Parse(input),
            static (IParser x, string input) => Contract.EnsuresOnThrow<FormatException>(input != null));

        Dbc.Def(static (IParser x, string input) => x.ParseNullAsFormatError(input),
            static (IParser x, string input) => Contract.EnsuresOnThrow<FormatException>(input != null, "input was null on throw"));
    }
}

[ContractClass(typeof(ThrowingContracts))]
public interface IThrowing
{
    int Balance { get; }

    void ThrowsEnsuresTrue(int exception, bool trueIfThrow);

    void ThrowsInvalidOperation();

    void Transfer(bool partial);
}

public sealed class Throwing : IThrowing
{
    public int Balance { get; private set; } = 10;

    public void ThrowsEnsuresTrue(int exception, bool trueIfThrow)
    {
        switch (exception)
        {
            case 1:
                throw new ApplicationException();
            case 2:
                throw new SystemException();
            case 3:
                throw new Exception();
        }
    }

    public void ThrowsInvalidOperation() => throw new InvalidOperationException();

    public void Transfer(bool partial)
    {
        if (partial)
        {
            Balance -= 5;
        }

        throw new InvalidOperationException("transfer failed");
    }
}

[ContractClassFor(typeof(IThrowing))]
internal sealed class ThrowingContracts
{
    public ThrowingContracts()
    {
        Dbc.Def(static (IThrowing x, int exception, bool trueIfThrow) => x.ThrowsEnsuresTrue(exception, trueIfThrow),
            static (IThrowing x, int exception, bool trueIfThrow) => Contract.EnsuresOnThrow<ApplicationException>(trueIfThrow),
            static (IThrowing x, int exception, bool trueIfThrow) => Contract.EnsuresOnThrow<SystemException>(trueIfThrow),
            static (IThrowing x, int exception, bool trueIfThrow) => Contract.EnsuresOnThrow<Exception>(trueIfThrow),
            static (IThrowing x, int exception, bool trueIfThrow) => Contract.Ensures(!trueIfThrow));

        Dbc.Def(static (IThrowing x) => x.ThrowsInvalidOperation(),
            static (IThrowing x) => Contract.EnsuresOnThrow<Exception>(true, "general"),
            static (IThrowing x) => Contract.EnsuresOnThrow<InvalidOperationException>(false, "specific"));

        Dbc.Def(static (IThrowing x, bool partial) => x.Transfer(partial),
            static (IThrowing x, bool partial) => Contract.EnsuresOnThrow<InvalidOperationException>(x.Balance == Contract.OldValue(x.Balance), "a failed transfer must not move money"));
    }
}
