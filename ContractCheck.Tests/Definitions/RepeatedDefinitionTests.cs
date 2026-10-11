#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Definitions.Repeated;

/// <summary>
/// Several <c>Dbc.Def</c> calls for one member add up: the clauses of every call are checked, in definition order.
/// Tools (and the dbc-property-tests skill) rely on this to add clauses without rewriting existing definitions.
/// </summary>
public class RepeatedDefinitionTests
{
    [Fact]
    public void Clauses_of_repeated_definitions_for_one_member_all_apply()
    {
        IDial proxy = Dbc.Make<IDial>(new Dial());

        proxy.Turn(5);

        Assert.Equal(5, proxy.Position);
        Assert.Equal("value >= 0", Expect.Violation(ContractFailureKind.Precondition, () => proxy.Turn(-1)).Condition);
        Assert.Equal("value <= 10", Expect.Violation(ContractFailureKind.Precondition, () => proxy.Turn(11)).Condition);
    }

    [Fact]
    public void Postconditions_of_a_later_definition_are_checked_too() =>
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IDial>(new Dial { Sticky = true }).Turn(3));
}

[ContractClass(typeof(DialContracts))]
public interface IDial
{
    int Position { get; }

    void Turn(int value);
}

public sealed class Dial : IDial
{
    public bool Sticky { get; init; }

    public int Position { get; private set; }

    public void Turn(int value) => Position = Sticky ? Position : value;
}

[ContractClassFor(typeof(IDial))]
internal sealed class DialContracts
{
    public DialContracts()
    {
        Dbc.Def(static (IDial x, int value) => x.Turn(value),
            static (IDial x, int value) => Contract.Requires(value >= 0));

        Dbc.Def(static (IDial x, int value) => x.Turn(value),
            static (IDial x, int value) => Contract.Requires(value <= 10),
            static (IDial x, int value) => Contract.Ensures(x.Position == value));
    }
}
