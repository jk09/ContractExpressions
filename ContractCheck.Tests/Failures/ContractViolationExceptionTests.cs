#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Failures.Exceptions;

/// <summary>
/// <c>ContractViolationException</c> and the failure helpers (resolutions R1, R2, R4): one public exception type whose
/// <c>Kind</c> is the BCL <c>ContractFailureKind</c> and whose message has the BCL display format plus the member.
/// </summary>
public class ContractViolationExceptionTests
{
    public static TheoryData<string, ContractFailureKind, string> Kinds => new()
    {
        { "Requires", ContractFailureKind.Precondition, "Precondition failed: value > 0  must be positive (in IKinds.Requires)" },
        { "Ensures", ContractFailureKind.Postcondition, "Postcondition failed: Contract.Result<int>() > 0  must be positive (in IKinds.Ensures)" },
        { "EnsuresOnThrow", ContractFailureKind.PostconditionOnException, "Postcondition failed after throwing an exception: value > 0  must be positive (in IKinds.EnsuresOnThrow)" },
        { "Invariant", ContractFailureKind.Invariant, "Invariant failed: value > 0  must be positive (in IKinds.Invariant)" },
        { "Assert", ContractFailureKind.Assert, "Assertion failed: value > 0  must be positive (in IKinds.Assert)" },
        { "Assume", ContractFailureKind.Assume, "Assumption failed: value > 0  must be positive (in IKinds.Assume)" },
    };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Every_clause_kind_reports_its_ContractFailureKind_and_message(string member, ContractFailureKind kind, string message)
    {
        IKinds proxy = Dbc.Make<IKinds>(new Kinds());

        ContractViolationException exception = Expect.Violation(kind, () => typeof(IKinds).GetMethod(member)!.Invoke(proxy, System.Reflection.BindingFlags.DoNotWrapExceptions, null, [0], null));

        Assert.Equal(message, exception.Message);
        Assert.Equal(member, exception.Method);
        Assert.Equal("must be positive", exception.UserMessage);
        Assert.Equal(typeof(IKinds), exception.InterfaceType);
        Assert.Equal(member, exception.Member!.Name);
        Assert.Equal(kind, exception.GetContractFailureKind());
    }

    [Fact]
    public void Entry_check_failures_are_recognised()
    {
        IKinds proxy = Dbc.Make<IKinds>(new Kinds());

        Assert.True(Assert.Throws<ContractViolationException>(() => proxy.Requires(0)).IsEntryCheckFailure());
        Assert.True(Assert.Throws<ContractViolationException>(() => proxy.Assert(0)).IsEntryCheckFailure());
        Assert.True(Assert.Throws<ContractViolationException>(() => proxy.Assume(0)).IsEntryCheckFailure());
        Assert.False(Assert.Throws<ContractViolationException>(() => proxy.Ensures(0)).IsEntryCheckFailure());
        Assert.False(Assert.Throws<ContractViolationException>(() => proxy.Invariant(0)).IsEntryCheckFailure());
    }

    [Fact]
    public void Other_exceptions_have_no_contract_failure_kind()
    {
        Assert.Null(new InvalidOperationException().GetContractFailureKind());
        Assert.False(new InvalidOperationException().IsEntryCheckFailure());
        Assert.Throws<ArgumentNullException>(() => ContractFailureExtensions.GetContractFailureKind(null!));
    }

    [Fact]
    public void Public_constructor_formats_a_message()
    {
        var exception = new ContractViolationException(ContractFailureKind.Precondition, "Add", "Contract.Requires(value > 0)");

        Assert.Equal(ContractFailureKind.Precondition, exception.Kind);
        Assert.Equal("Add", exception.Method);
        Assert.Equal("Contract.Requires(value > 0)", exception.ContractText);
        Assert.Equal("Precondition failed: Contract.Requires(value > 0) (in Add)", exception.Message);
    }

    [Fact]
    public void Failure_never_leaves_an_unwind_handler_behind()
    {
        // ContractExpressions subscribed a SetUnwind handler on every check (a leak that also changed how all later
        // contract failures in the process behave). After many ContractCheck failures, the BCL event still has no
        // handler forcing an unwind, so an unhandled failure is reported, not thrown.
        IKinds proxy = Dbc.Make<IKinds>(new Kinds());
        for (int i = 0; i < 20; i++)
        {
            Assert.Throws<ContractViolationException>(() => proxy.Requires(0));
        }

        string? message = System.Runtime.CompilerServices.ContractHelper.RaiseContractFailedEvent(ContractFailureKind.Assert, null, "probe", null);
        Assert.Equal("Assertion failed: probe", message);
    }
}

[ContractClass(typeof(KindsContracts))]
public interface IKinds
{
    void Requires(int value);

    int Ensures(int value);

    void EnsuresOnThrow(int value);

    void Invariant(int value);

    void Assert(int value);

    void Assume(int value);
}

public sealed class Kinds : IKinds
{
    public void Requires(int value)
    {
    }

    public int Ensures(int value) => value;

    public void EnsuresOnThrow(int value) => throw new InvalidOperationException();

    public void Invariant(int value)
    {
    }

    public void Assert(int value)
    {
    }

    public void Assume(int value)
    {
    }
}

[ContractClassFor(typeof(IKinds))]
internal sealed class KindsContracts
{
    public KindsContracts()
    {
        Dbc.Def(static (IKinds x, int value) => x.Requires(value), static (IKinds x, int value) => Contract.Requires(value > 0, "must be positive"));
        Dbc.Def(static (IKinds x, int value) => x.Ensures(value), static (IKinds x, int value) => Contract.Ensures(Contract.Result<int>() > 0, "must be positive"));
        Dbc.Def(static (IKinds x, int value) => x.EnsuresOnThrow(value), static (IKinds x, int value) => Contract.EnsuresOnThrow<InvalidOperationException>(value > 0, "must be positive"));
        Dbc.Def(static (IKinds x, int value) => x.Invariant(value), static (IKinds x, int value) => Contract.Invariant(value > 0, "must be positive"));
        Dbc.Def(static (IKinds x, int value) => x.Assert(value), static (IKinds x, int value) => Contract.Assert(value > 0, "must be positive"));
        Dbc.Def(static (IKinds x, int value) => x.Assume(value), static (IKinds x, int value) => Contract.Assume(value > 0, "must be positive"));
    }
}
