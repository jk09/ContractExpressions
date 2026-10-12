#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractCheck.FsCheck;
using FsCheck;
using FsCheck.Fluent;

namespace ContractCheck.Tests.PropertyTesting.Calls;

/// <summary>
/// <c>DbcPropertyTest.CheckCalls</c>: random call sequences generated from the interface itself – no hand-written steps.
/// </summary>
public class CheckCallsTests
{
    private static readonly Config Strict = Config.QuickThrowOnFailure.WithMaxTest(200);

    [Fact]
    public void Finds_an_invariant_broken_by_a_sequence_of_calls_and_prints_the_call_log()
    {
        Exception failure = Assert.ThrowsAny<Exception>(() => Check.One(Strict, DbcPropertyTest.CheckCalls(() => Dbc.Make<ITank>(new Tank()))));

        Assert.Contains("Falsifiable", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Invariant failed: x.Level >= 0 && x.Level <= 100", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Fill(true)", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Calls_rejected_by_preconditions_are_skipped() =>
        Check.One(Strict, DbcPropertyTest.CheckCalls(() => Dbc.Make<ITank>(new Tank()), new CallOptions { Exclude = ["Fill"] }));

    [Fact]
    public void Undocumented_exceptions_fail_and_documented_ones_do_not()
    {
        Func<IVault> vault = () => Dbc.Make<IVault>(new Vault());

        Assert.Contains(nameof(InvalidOperationException), Assert.ThrowsAny<Exception>(() => Check.One(Strict, DbcPropertyTest.CheckCalls(vault))).Message, StringComparison.Ordinal);
        Check.One(Strict, DbcPropertyTest.CheckCalls(vault, new CallOptions { ExpectedExceptions = [typeof(InvalidOperationException)] }));
    }

    [Fact]
    public void An_expected_exception_still_has_its_EnsuresOnThrow_checked()
    {
        Property property = DbcPropertyTest.CheckCalls(
            () => Dbc.Make<IVault>(new Vault { LeakOnFailure = true }),
            new CallOptions { ExpectedExceptions = [typeof(InvalidOperationException)] });

        Assert.Contains("Postcondition failed after throwing an exception", Assert.ThrowsAny<Exception>(() => Check.One(Strict, property)).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Async_members_are_awaited_so_their_postconditions_are_checked() =>
        Assert.Contains(
            "Postcondition failed: x.Total == Contract.OldValue(x.Total) + 1",
            Assert.ThrowsAny<Exception>(() => Check.One(Strict, DbcPropertyTest.CheckCalls(() => Dbc.Make<ICounterService>(new CounterService())))).Message,
            StringComparison.Ordinal);

    [Fact]
    public void Members_without_generatable_arguments_are_left_out() =>
        Assert.Equal(
            ["IncrementAsync()", "Name", "Name = string", "Reset(int)", "Total", "this[string]", "this[string] = int"],
            DbcPropertyTest.CallableMembers<ICounterService>().Order(StringComparer.Ordinal));

    [Fact]
    public void Exclude_removes_members_by_name() =>
        Assert.Equal(
            ["IncrementAsync()", "Name", "Name = string", "Reset(int)", "Total"],
            DbcPropertyTest.CallableMembers<ICounterService>(new CallOptions { Exclude = ["Item"] }).Order(StringComparer.Ordinal));

    [Fact]
    public void An_interface_without_callable_members_is_rejected() =>
        Assert.Throws<InvalidOperationException>(() => DbcPropertyTest.CheckCalls(() => Dbc.Make<INothingCallable>(new NothingCallable())));
}

[ContractClass(typeof(TankContracts))]
public interface ITank
{
    int Level { get; }

    void Fill(bool full);

    void Drain(int amount);
}

/// <summary>Each Fill(true) keeps its postcondition, but two of them overflow the invariant's limit of 100.</summary>
public sealed class Tank : ITank
{
    public int Level { get; private set; }

    public void Fill(bool full) => Level += full ? 60 : 0;

    public void Drain(int amount) => Level -= amount;
}

[ContractClassFor(typeof(ITank))]
internal sealed class TankContracts
{
    public TankContracts()
    {
        Dbc.Def(static (ITank x, bool full) => x.Fill(full), static (ITank x, bool full) => Contract.Ensures(x.Level >= Contract.OldValue(x.Level)));
        Dbc.Def(static (ITank x, int amount) => x.Drain(amount),
            static (ITank x, int amount) => Contract.Requires(amount > 0 && amount <= x.Level),
            static (ITank x, int amount) => Contract.Ensures(x.Level == Contract.OldValue(x.Level) - amount));
        Dbc.Def(static (ITank x) => Contract.Invariant(x.Level >= 0 && x.Level <= 100));
    }
}

[ContractClass(typeof(VaultContracts))]
public interface IVault
{
    int Coins { get; }

    void Deposit(int coins);

    /// <summary>Throws <see cref="InvalidOperationException"/> when the vault holds fewer coins.</summary>
    void Withdraw(int coins);
}

public sealed class Vault : IVault
{
    public bool LeakOnFailure { get; init; }

    public int Coins { get; private set; }

    public void Deposit(int coins) => Coins += coins;

    public void Withdraw(int coins)
    {
        if (coins > Coins)
        {
            Coins -= LeakOnFailure ? 1 : 0;
            throw new InvalidOperationException("Not enough coins.");
        }

        Coins -= coins;
    }
}

[ContractClassFor(typeof(IVault))]
internal sealed class VaultContracts
{
    public VaultContracts()
    {
        Dbc.Def(static (IVault x, int coins) => x.Deposit(coins), static (IVault x, int coins) => Contract.Requires(coins > 0));
        Dbc.Def(static (IVault x, int coins) => x.Withdraw(coins),
            static (IVault x, int coins) => Contract.Requires(coins > 0),
            static (IVault x, int coins) => Contract.EnsuresOnThrow<InvalidOperationException>(x.Coins == Contract.OldValue(x.Coins)));
    }
}

[ContractClass(typeof(CounterServiceContracts))]
public interface ICounterService
{
    int Total { get; }

    int this[string key] { get; set; }

    string Name { get; set; }

    Task IncrementAsync();

    void Reset(int total);

    bool TryRead(string key, out int value);

    T Echo<T>(T value);

    void Attach(IDisposable resource);

    event EventHandler Changed;
}

/// <summary>IncrementAsync adds 2 after an await, which only the awaited postcondition can see.</summary>
public sealed class CounterService : ICounterService
{
    private readonly Dictionary<string, int> values = [];

    public event EventHandler? Changed;

    public int Total { get; private set; }

    public string Name { get; set; } = string.Empty;

    public int this[string key]
    {
        get => values.GetValueOrDefault(key);
        set => values[key] = value;
    }

    public async Task IncrementAsync()
    {
        await Task.Yield();
        Total += 2;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Reset(int total) => Total = total;

    public bool TryRead(string key, out int value) => values.TryGetValue(key, out value);

    public T Echo<T>(T value) => value;

    public void Attach(IDisposable resource)
    {
    }
}

[ContractClassFor(typeof(ICounterService))]
internal sealed class CounterServiceContracts
{
    public CounterServiceContracts() =>
        Dbc.Def(static (ICounterService x) => x.IncrementAsync(), static (ICounterService x) => Contract.Ensures(x.Total == Contract.OldValue(x.Total) + 1));
}

public interface INothingCallable
{
    void Attach(IDisposable resource);
}

public sealed class NothingCallable : INothingCallable
{
    public void Attach(IDisposable resource)
    {
    }
}
