#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Clauses.Requires;

/// <summary>
/// <c>Contract.Requires(bool)</c> and <c>Contract.Requires(bool, string)</c>.
/// Includes the four precondition overload scenarios of ContractExpressions.Tests/PreconditionOverloadTests.cs and
/// ContractExpressions4.Tests/PreconditionOverloadTests.cs (the typed ones are in <see cref="TypedRequiresTests"/>).
/// </summary>
public class RequiresTests
{
    [Fact]
    public void Satisfied_precondition_lets_the_call_through()
    {
        var target = new Gate();

        Dbc.Make<IGate>(target).Open("door");

        Assert.Equal("door", target.LastOpened);
    }

    [Fact]
    public void Violated_precondition_throws_before_the_member_runs()
    {
        var target = new Gate();

        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IGate>(target).Open(null!));

        Assert.Null(target.LastOpened);
        Assert.Equal("Open", exception.Method);
        Assert.Equal("name != null", exception.Condition);
        Assert.Null(exception.UserMessage);
        Assert.Equal("Precondition failed: name != null (in IGate.Open)", exception.Message);
    }

    [Fact]
    public void User_message_is_part_of_the_failure()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IGate>(new Gate()).Hang(null!));

        Assert.Equal("Object cannot be null", exception.UserMessage);
        Assert.Contains("Object cannot be null", exception.Message, StringComparison.Ordinal);
        Assert.Equal("Precondition failed: item != null  Object cannot be null (in IGate.Hang)", exception.Message);
    }

    [Fact]
    public void Message_can_be_computed_from_the_arguments_and_is_evaluated_only_on_failure()
    {
        IGate proxy = Dbc.Make<IGate>(new Gate());
        int before = GateContracts.MessagesBuilt;

        proxy.Resize(10);
        Assert.Equal(before, GateContracts.MessagesBuilt);

        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => proxy.Resize(1000));
        Assert.Equal("size 1000 exceeds 100", exception.UserMessage);
    }

    [Fact]
    public void Preconditions_are_evaluated_in_definition_order_and_the_first_failure_is_reported()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IGate>(new Gate()).Rename(null!, 0));

        Assert.Equal("name != null", exception.Condition);
    }

    [Fact]
    public void Precondition_can_read_the_target_state()
    {
        var target = new Gate { Locked = true };

        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<IGate>(target).Open("door"));
    }

    [Fact]
    public void Precondition_can_call_params_methods_ported_from_UseAParamsMethodInContract()
    {
        // CodeContracts Foxtrot/Tests/RewrittenContractTest.cs: Positive/NegativeUseAParamsMethodInContract.
        IGate proxy = Dbc.Make<IGate>(new Gate());

        Assert.Equal(4, proxy.UseParams(3));
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.UseParams(5));
    }

    [Fact]
    public void Tautological_precondition_never_fails_ported_from_RequiresAnything()
    {
        IGate proxy = Dbc.Make<IGate>(new Gate());

        proxy.Anything(true);
        proxy.Anything(false);
    }
}

[ContractClass(typeof(GateContracts))]
public interface IGate
{
    bool Locked { get; }

    void Open(string name);

    void Hang(object item);

    void Resize(int size);

    void Rename(string name, int id);

    int UseParams(int x);

    void Anything(bool anything);
}

public sealed class Gate : IGate
{
    public bool Locked { get; set; }

    public string? LastOpened { get; private set; }

    public void Open(string name) => LastOpened = name;

    public void Hang(object item)
    {
    }

    public void Resize(int size)
    {
    }

    public void Rename(string name, int id)
    {
    }

    public int UseParams(int x) => x + 1;

    public void Anything(bool anything)
    {
    }
}

[ContractClassFor(typeof(IGate))]
internal sealed class GateContracts
{
    private static int messagesBuilt;

    public GateContracts()
    {
        Dbc.Def(static (IGate x, string name) => x.Open(name),
            static (IGate x, string name) => Contract.Requires(name != null),
            static (IGate x, string name) => Contract.Requires(!x.Locked, "the gate is locked"));

        Dbc.Def(static (IGate x, object item) => x.Hang(item),
            static (IGate x, object item) => Contract.Requires(item != null, "Object cannot be null"));

        Dbc.Def(static (IGate x, int size) => x.Resize(size),
            static (IGate x, int size) => Contract.Requires(size <= 100, BuildMessage(size)));

        Dbc.Def(static (IGate x, string name, int id) => x.Rename(name, id),
            static (IGate x, string name, int id) => Contract.Requires(name != null),
            static (IGate x, string name, int id) => Contract.Requires(id > 0));

        Dbc.Def(static (IGate x, int value) => x.UseParams(value),
            static (IGate x, int value) => Contract.Requires(TakesParams(value) == value));

        Dbc.Def(static (IGate x, bool anything) => x.Anything(anything),
            static (IGate x, bool anything) => Contract.Requires(anything || !anything));
    }

    public static int MessagesBuilt => Volatile.Read(ref messagesBuilt);

    [Pure]
    private static int TakesParams(params int[] values) => 3;

    private static string BuildMessage(int size)
    {
        Interlocked.Increment(ref messagesBuilt);
        return $"size {size} exceeds 100";
    }
}
