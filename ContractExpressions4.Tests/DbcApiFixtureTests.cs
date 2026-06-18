#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;
using ContractExpressions4.Check;

namespace ContractExpressions4.Tests;

public class DbcApiFixtureTests
{
    [Fact]
    public void Add_WithValidInput_SatisfiesContracts()
    {
        ICounter proxy = Dbc.Make<ICounter>(new Counter());

        int result = proxy.Add(2);

        Assert.Equal(2, result);
        Assert.Equal(2, proxy.Count);
    }

    [Fact]
    public void Add_WhenPreconditionFails_ThrowsPreconditionViolation()
    {
        ICounter proxy = Dbc.Make<ICounter>(new Counter());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Add(0));

        Assert.Equal(ContractKind.Precondition, ex.Kind);
        Assert.Equal("Add", ex.Method);
    }

    [Fact]
    public void Add_WhenPostconditionFails_ThrowsPostconditionViolation()
    {
        IBrokenCounter proxy = Dbc.Make<IBrokenCounter>(new BrokenCounter());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Add(2));

        Assert.Equal(ContractKind.Postcondition, ex.Kind);
        Assert.Equal("Add", ex.Method);
    }

    // ── Property-based tests (FsCheck) ────────────────────────────────────────
    // DbcPropertyTest.Check creates a fresh proxy per test case, which:
    //   1. Validates invariants at creation (proxyFactory() call).
    //   2. Discards test cases where the precondition is not satisfied (inconclusive).
    //   3. Validates postconditions and invariants after the method call.

    [Property(QuietOnSuccess = false, Verbose = true)]
    public Property Add_RandomInputs_SatisfiesContracts(int value) =>
        DbcPropertyTest.Check(
            () => Dbc.Make<ICounter>(new Counter()),
            (ICounter proxy) => proxy.Add(value));

    [Fact]
    public void Make_WhenInvariantFailsAtCreation_ThrowsInvariantViolation()
    {
        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => Dbc.Make<IInvariantCounter>(new InvalidInvariantCounter()));

        Assert.Equal(ContractKind.Invariant, ex.Kind);
        Assert.Equal("<creation>", ex.Method);
    }

    [Fact]
    public void Spend_WhenInvariantFailsAfterMethod_ThrowsInvariantViolation()
    {
        IInvariantCounter proxy = Dbc.Make<IInvariantCounter>(new ValidInvariantCounter());

        ContractViolationException ex = Assert.Throws<ContractViolationException>(() => proxy.Spend(2));

        Assert.Equal(ContractKind.Invariant, ex.Kind);
        Assert.Equal("Spend", ex.Method);
    }
}

[ContractClass(typeof(CounterContracts))]
interface ICounter
{
    int Count { get; }
    int Add(int value);
}

class Counter : ICounter
{
    public int Count { get; private set; }

    public int Add(int value)
    {
        Count += value;
        return Count;
    }
}

[ContractClassFor(typeof(ICounter))]
class CounterContracts
{
    public CounterContracts()
    {
        Dbc.Def(static (ICounter x, int value) => x.Add(value),
            static (ICounter x, int value) => Contract.Requires(value > 0),
            static (ICounter x, int value) => Contract.Ensures(Contract.Result<int>() == Contract.OldValue<int>(x.Count) + value),
            static (ICounter x, int value) => Contract.Ensures(x.Count == Contract.Result<int>()));

        Dbc.Def(static (ICounter x) => Contract.Invariant(x.Count >= 0));
    }
}

[ContractClass(typeof(BrokenCounterContracts))]
interface IBrokenCounter
{
    int Count { get; }
    int Add(int value);
}

class BrokenCounter : IBrokenCounter
{
    public int Count { get; private set; }

    public int Add(int value)
    {
        Count += value;
        return Count + 1;
    }
}

[ContractClassFor(typeof(IBrokenCounter))]
class BrokenCounterContracts
{
    public BrokenCounterContracts()
    {
        Dbc.Def(static (IBrokenCounter x, int value) => x.Add(value),
            static (IBrokenCounter x, int value) => Contract.Ensures(Contract.Result<int>() == x.Count));
    }
}

[ContractClass(typeof(InvariantCounterContracts))]
interface IInvariantCounter
{
    int Balance { get; }
    void Spend(int amount);
}

class InvalidInvariantCounter : IInvariantCounter
{
    public int Balance => -1;

    public void Spend(int amount)
    {
    }
}

class ValidInvariantCounter : IInvariantCounter
{
    public int Balance { get; private set; } = 1;

    public void Spend(int amount)
    {
        Balance -= amount;
    }
}

[ContractClassFor(typeof(IInvariantCounter))]
class InvariantCounterContracts
{
    public InvariantCounterContracts()
    {
        Dbc.Def(static (IInvariantCounter x) => Contract.Invariant(x.Balance >= 0));
    }
}
