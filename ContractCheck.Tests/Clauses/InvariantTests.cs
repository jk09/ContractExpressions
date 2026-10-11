#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Clauses.Invariants;

/// <summary>
/// <c>Contract.Invariant</c> (resolutions R8, R9).
/// Interface invariants – <c>Dbc.Def(static (T x) =&gt; Contract.Invariant(...))</c> – are checked by <c>Dbc.Make</c> and after
/// every member that returns normally, except pure members (property getters, <c>[Pure]</c>), like CodeContracts object invariants.
/// An <c>Invariant</c> clause attached to a member is checked when that member returns.
/// ContractExpressions ignored the interface form; ContractExpressions4 also checked on entry and promoted member invariants
/// to interface invariants.
/// </summary>
public class InvariantTests
{
    [Fact]
    public void Interface_invariant_is_checked_when_the_proxy_is_made()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Invariant, () => Dbc.Make<IWallet>(new Wallet(-1)));

        Assert.Equal(ContractViolationException.CreationMethodName, exception.Method);
        Assert.Null(exception.Member);
        Assert.Equal("Invariant failed: x.Balance >= 0  balance must not be negative (in IWallet, checked by Dbc.Make)", exception.Message);
    }

    [Fact]
    public void Interface_invariant_is_checked_after_a_member_that_breaks_it()
    {
        IWallet proxy = Dbc.Make<IWallet>(new Wallet(1));

        ContractViolationException exception = Expect.Violation(ContractFailureKind.Invariant, () => proxy.Spend(2));

        Assert.Equal("Spend", exception.Method);
    }

    [Fact]
    public void Several_invariants_can_be_defined_in_one_interface_level_Def()
    {
        Expect.Violation(ContractFailureKind.Invariant, () => Dbc.Make<IWallet>(new Wallet(1) { Limit = 200 }));
    }

    [Fact]
    public void Interface_invariant_is_not_checked_after_property_getters()
    {
        var target = new Wallet(1);
        IWallet proxy = Dbc.Make<IWallet>(target);
        target.ForceBalance(-5);

        Assert.Equal(-5, proxy.Balance);
        Expect.Violation(ContractFailureKind.Invariant, () => proxy.Deposit(1));
    }

    [Fact]
    public void Interface_invariant_is_not_checked_after_Pure_members()
    {
        var target = new Wallet(1);
        IWallet proxy = Dbc.Make<IWallet>(target);
        target.ForceBalance(-5);

        Assert.Equal("-5", proxy.Describe());
    }

    [Fact]
    public void Interface_invariant_is_not_checked_on_entry()
    {
        // ContractExpressions4 checked invariants before every call; CodeContracts did not.
        var target = new Wallet(1);
        IWallet proxy = Dbc.Make<IWallet>(target);
        target.ForceBalance(-5);

        proxy.Deposit(10);

        Assert.Equal(5, proxy.Balance);
    }

    [Fact]
    public void Interface_invariant_is_not_checked_when_the_member_throws()
    {
        var target = new Wallet(1);
        IWallet proxy = Dbc.Make<IWallet>(target);

        Assert.Throws<InvalidOperationException>(() => proxy.SpendAndFail(5));
        Assert.Equal(-4, target.Balance);
    }

    [Fact]
    public void Interface_invariant_is_checked_after_property_setters_ported_from_NegativeInvariantTest()
    {
        // CodeContracts Foxtrot/Tests/RewrittenContractTest.cs: setting MustBeTrue = false breaks the object invariant.
        IWallet proxy = Dbc.Make<IWallet>(new Wallet(1));

        proxy.MustBeTrue = true;
        Expect.Violation(ContractFailureKind.Invariant, () => proxy.MustBeTrue = false);
    }

    [Fact]
    public void Invariant_that_calls_a_public_member_does_not_recurse_ported_from_ReEntrantInvariantTest()
    {
        // Clauses run against the target, not the proxy, so an invariant may call members without re-checking itself.
        IWallet proxy = Dbc.Make<IWallet>(new Wallet(1));

        proxy.Deposit(1);

        Assert.Equal(2, proxy.Balance);
    }

    [Fact]
    public void Member_invariant_is_checked_after_that_member_only()
    {
        // Probe S08: ContractExpressions4 promoted this clause to an interface invariant and failed on Decrement.
        ITally proxy = Dbc.Make<ITally>(new Tally());

        proxy.Decrement();
        Assert.Equal(-1, proxy.Value);

        Expect.Violation(ContractFailureKind.Invariant, () => proxy.Increment());
    }

    [Fact]
    public void Member_invariant_is_not_checked_when_the_proxy_is_made()
    {
        // Probe S09.
        Assert.Equal(-5, Dbc.Make<ITally>(new Tally(-5)).Value);
    }

    [Fact]
    public void Postconditions_are_checked_before_invariants()
    {
        // Overdraw breaks both its postcondition and the balance invariant; the legacy order reports the postcondition.
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IWallet>(new Wallet(5)).Overdraw(10));

        Assert.Equal("x.Balance >= 0", exception.Condition);
    }
}

/// <summary>Ported from ContractExpressions(4).Tests/CE-1/InvariantTests.cs: invariants attached to Increment and Decrement.</summary>
public class CounterInvariantTests
{
    [Fact]
    public void Increment_preserves_invariant()
    {
        ICounter proxy = Dbc.Make<ICounter>(new Counter());

        proxy.Increment();

        Assert.Equal(1, proxy.Value);
    }

    [Fact]
    public void Decrement_to_zero_preserves_invariant()
    {
        ICounter proxy = Dbc.Make<ICounter>(new Counter());

        proxy.Increment();
        proxy.Decrement();

        Assert.Equal(0, proxy.Value);
    }

    [Fact]
    public void Decrement_below_zero_violates_invariant()
    {
        Expect.Violation(ContractFailureKind.Invariant, () => Dbc.Make<ICounter>(new Counter()).Decrement());
    }

    [Fact]
    public void Invariant_message_appears_in_the_exception()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Invariant, () => Dbc.Make<ICounter>(new Counter()).DecrementWithMessage());

        Assert.Contains("Counter must be non-negative", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Multiple_increments_keep_the_invariant()
    {
        ICounter proxy = Dbc.Make<ICounter>(new Counter());

        for (int i = 0; i < 10; i++)
        {
            proxy.Increment();
        }

        Assert.Equal(10, proxy.Value);
    }
}

[ContractClass(typeof(WalletContracts))]
public interface IWallet
{
    int Balance { get; }

    int Limit { get; }

    bool MustBeTrue { get; set; }

    void Deposit(int amount);

    void Spend(int amount);

    void SpendAndFail(int amount);

    void Overdraw(int amount);

    [Pure]
    string Describe();
}

public sealed class Wallet(int balance) : IWallet
{
    public int Balance { get; private set; } = balance;

    public int Limit { get; init; } = 100;

    public bool MustBeTrue { get; set; } = true;

    public void Deposit(int amount) => Balance += amount;

    public void Spend(int amount) => Balance -= amount;

    public void SpendAndFail(int amount)
    {
        Balance -= amount;
        throw new InvalidOperationException("spent, then failed");
    }

    public void Overdraw(int amount) => Balance -= amount;

    public string Describe() => Balance.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public void ForceBalance(int balance) => Balance = balance;
}

[ContractClassFor(typeof(IWallet))]
internal sealed class WalletContracts
{
    public WalletContracts()
    {
        Dbc.Def(static (IWallet x) => Contract.Invariant(x.Balance >= 0, "balance must not be negative"),
            static (IWallet x) => Contract.Invariant(x.Limit <= 100),
            static (IWallet x) => Contract.Invariant(x.MustBeTrue));

        Dbc.Def(static (IWallet x) => Contract.Invariant(x.Describe() != null));

        Dbc.Def(static (IWallet x, int amount) => x.Spend(amount),
            static (IWallet x, int amount) => Contract.Ensures(x.Balance == Contract.OldValue(x.Balance) - amount));

        Dbc.Def(static (IWallet x, int amount) => x.Overdraw(amount),
            static (IWallet x, int amount) => Contract.Ensures(x.Balance >= 0));
    }
}

[ContractClass(typeof(TallyContracts))]
public interface ITally
{
    int Value { get; }

    void Increment();

    void Decrement();
}

public sealed class Tally(int value = 0) : ITally
{
    public int Value { get; private set; } = value;

    public void Increment() => Value++;

    public void Decrement() => Value--;
}

[ContractClassFor(typeof(ITally))]
internal sealed class TallyContracts
{
    public TallyContracts()
    {
        Dbc.Def(static (ITally x) => x.Increment(),
            static (ITally x) => Contract.Invariant(x.Value > 0));
    }
}

[ContractClass(typeof(CounterContracts))]
public interface ICounter
{
    int Value { get; }

    void Increment();

    void Decrement();

    void DecrementWithMessage();
}

public sealed class Counter : ICounter
{
    public int Value { get; private set; }

    public void Increment() => Value++;

    public void Decrement() => Value--;

    public void DecrementWithMessage() => Value--;
}

[ContractClassFor(typeof(ICounter))]
internal sealed class CounterContracts
{
    public CounterContracts()
    {
        Dbc.Def(static (ICounter x) => x.Increment(),
            static (ICounter x) => Contract.Invariant(x.Value >= 0));

        Dbc.Def(static (ICounter x) => x.Decrement(),
            static (ICounter x) => Contract.Invariant(x.Value >= 0));

        Dbc.Def(static (ICounter x) => x.DecrementWithMessage(),
            static (ICounter x) => Contract.Invariant(x.Value >= 0, "Counter must be non-negative"));
    }
}
