#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions;

namespace ContractExpressions.Tests.CE1;

/// <summary>
/// Dedicated tests for Contract.Invariant — first test coverage.
/// Inspired by BankAccount invariant pattern ([ContractInvariantMethod]).
/// Covers: Invariant(bool), Invariant(bool, string), multiple invariants on same method.
/// </summary>
public class InvariantTests : IClassFixture<ContractFailureUnwindFixture>
{
    [Fact]
    public void Increment_PreservesInvariant()
    {
        var proxy = Dbc.Make<ICounter>(new Counter());
        proxy.Increment();
        Assert.Equal(1, proxy.Value);
    }

    [Fact]
    public void Decrement_ToZero_PreservesInvariant()
    {
        var proxy = Dbc.Make<ICounter>(new Counter());
        proxy.Increment();
        proxy.Decrement();
        Assert.Equal(0, proxy.Value);
    }

    [Fact]
    public void Decrement_BelowZero_ViolatesInvariant()
    {
        var proxy = Dbc.Make<ICounter>(new Counter());
        var ex = Assert.ThrowsAny<Exception>(() => proxy.Decrement());
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
    }

    [Fact]
    public void InvariantWithMessage_MessageAppearsInException()
    {
        var proxy = Dbc.Make<ICounterWithMessage>(new CounterForMessage());
        var ex = Assert.ThrowsAny<Exception>(() => proxy.Decrement());
        Assert.Equal("System.Diagnostics.Contracts.ContractException", ex.GetType().FullName);
        Assert.Contains("Counter must be non-negative", ex.Message);
    }

    [Fact]
    public void MultipleIncrements_InvariantHoldsEachTime()
    {
        var proxy = Dbc.Make<ICounter>(new Counter());
        for (int i = 0; i < 10; i++)
        {
            proxy.Increment();
        }
        Assert.Equal(10, proxy.Value);
    }
}

// --- ICounter: invariant without message ---

[ContractClass(typeof(CounterContracts))]
interface ICounter
{
    int Value { get; }
    void Increment();
    void Decrement();
}

class Counter : ICounter
{
    public int Value { get; private set; }

    public void Increment() => Value++;
    public void Decrement() => Value--;
}

[ContractClassFor(typeof(ICounter))]
class CounterContracts
{
    public CounterContracts()
    {
        Dbc.Def(static (ICounter x) => x.Increment(),
                static (ICounter x) => Contract.Invariant(x.Value >= 0));

        Dbc.Def(static (ICounter x) => x.Decrement(),
                static (ICounter x) => Contract.Invariant(x.Value >= 0));
    }
}

// --- ICounterWithMessage: invariant with message string ---

[ContractClass(typeof(CounterWithMessageContracts))]
interface ICounterWithMessage
{
    int Value { get; }
    void Increment();
    void Decrement();
}

class CounterForMessage : ICounterWithMessage
{
    public int Value { get; private set; }

    public void Increment() => Value++;
    public void Decrement() => Value--;
}

[ContractClassFor(typeof(ICounterWithMessage))]
class CounterWithMessageContracts
{
    public CounterWithMessageContracts()
    {
        Dbc.Def(static (ICounterWithMessage x) => x.Increment(),
                static (ICounterWithMessage x) => Contract.Invariant(x.Value >= 0, "Counter must be non-negative"));

        Dbc.Def(static (ICounterWithMessage x) => x.Decrement(),
                static (ICounterWithMessage x) => Contract.Invariant(x.Value >= 0, "Counter must be non-negative"));
    }
}
