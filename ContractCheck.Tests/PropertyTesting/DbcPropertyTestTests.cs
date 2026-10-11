#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractCheck.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace ContractCheck.Tests.PropertyTesting;

/// <summary>
/// Behaviour of <c>DbcPropertyTest</c> (ContractCheck.FsCheck): entry-check failures discard a generated case,
/// postcondition and invariant failures fail the property, tasks are awaited, and sequences share one proxy.
/// </summary>
public class DbcPropertyTestTests
{
    private static readonly Config Strict = Config.QuickThrowOnFailure.WithMaxTest(200);

    [Fact]
    public void Precondition_failures_are_discarded()
    {
        Property property = Prop.ForAll((int value) => DbcPropertyTest.Check(() => Dbc.Make<IGauge>(new Gauge()), g => g.Set(value)));

        Check.One(Strict, property);
    }

    [Fact]
    public void Typed_precondition_failures_are_discarded()
    {
        Property property = Prop.ForAll((string? name) => DbcPropertyTest.Check(() => Dbc.Make<IGauge>(new Gauge()), g => g.Rename(name!)));

        Check.One(Strict, property);
    }

    [Fact]
    public void Postcondition_failure_fails_the_property()
    {
        Property property = Prop.ForAll((int value) => DbcPropertyTest.Check(() => Dbc.Make<IGauge>(new Gauge { Broken = true }), g => g.Set(value)));

        AssertFalsified(property, "Postcondition failed: x.Level == value");
    }

    [Fact]
    public void Invariant_failure_at_creation_fails_the_property()
    {
        Property property = Prop.ForAll((int value) => DbcPropertyTest.Check(() => Dbc.Make<IGauge>(new Gauge(-1)), g => g.Set(value)));

        AssertFalsified(property, "Invariant failed: x.Level >= 0 && x.Level <= 1000");
    }

    [Fact]
    public void Other_exceptions_fail_the_property()
    {
        Property property = Prop.ForAll((int value) => DbcPropertyTest.Check(() => Dbc.Make<IGauge>(new Gauge()), g => g.Crash()));

        AssertFalsified(property, nameof(NotImplementedException));
    }

    [Fact]
    public void Returned_tasks_are_awaited_so_async_postconditions_are_checked()
    {
        Property viaCheck = Prop.ForAll((int value) => DbcPropertyTest.Check(() => Dbc.Make<IGauge>(new Gauge { Broken = true }), g => g.SetAsync(value)));
        Property viaCheckAsync = Prop.ForAll((int value) => DbcPropertyTest.CheckAsync(() => Dbc.Make<IGauge>(new Gauge { Broken = true }), g => g.SetAsync(value)));
        Property viaValueTask = Prop.ForAll((int value) => DbcPropertyTest.Check(() => Dbc.Make<IGauge>(new Gauge { Broken = true }), g => g.ReadAsync(value)));

        AssertFalsified(viaCheck, "Postcondition failed: x.Level == value");
        AssertFalsified(viaCheckAsync, "Postcondition failed: x.Level == value");
        AssertFalsified(viaValueTask, "Postcondition failed: Contract.Result<int>() == value");
    }

    [Fact]
    public void Correct_async_members_pass()
    {
        Check.One(Strict, Prop.ForAll((int value) => DbcPropertyTest.CheckAsync(() => Dbc.Make<IGauge>(new Gauge()), g => g.SetAsync(value))));
        Check.One(Strict, Prop.ForAll((int value) => DbcPropertyTest.Check(() => Dbc.Make<IGauge>(new Gauge()), g => g.ReadAsync(value))));
    }

    [Fact]
    public void Async_lambdas_bind_to_CheckAsync()
    {
        // An async lambda converts to both Task and ValueTask delegates; CheckAsync only takes Task ones, so these calls
        // are unambiguous on every C# version (C# 12, the net8.0 default, has no [OverloadResolutionPriority]).
        Check.One(Strict, Prop.ForAll((int value) => DbcPropertyTest.CheckAsync(() => Dbc.Make<IGauge>(new Gauge()), async g => await g.SetAsync(value))));
        Check.One(Strict, Prop.ForAll((int value) => DbcPropertyTest.CheckAsync(() => Dbc.Make<IGauge>(new Gauge()), async g => await g.ReadAsync(value))));
        AssertFalsified(
            Prop.ForAll((int value) => DbcPropertyTest.CheckAsync(() => Dbc.Make<IGauge>(new Gauge { Broken = true }), async g => await g.ReadAsync(value))),
            "Postcondition failed: Contract.Result<int>() == value");
    }

    [Fact]
    public void Sequence_skips_rejected_steps_and_keeps_going()
    {
        Property property = Prop.ForAll((int[] values) => DbcPropertyTest.CheckSequence(
            () => Dbc.Make<IGauge>(new Gauge()),
            values.Select(value => (Action<IGauge>)(g => g.Set(value)))));

        Check.One(Strict, property);
    }

    [Fact]
    public void Sequence_finds_an_invariant_broken_by_a_combination_of_steps()
    {
        // Each Bump keeps its postcondition, but three bumps (1200) exceed the invariant's limit of 1000.
        Property property = Prop.ForAll((bool[] bumps) => DbcPropertyTest.CheckSequence(
            () => Dbc.Make<IGauge>(new Gauge()),
            bumps.Select(bump => (Action<IGauge>)(g => g.Bump(bump)))));

        AssertFalsified(property, "Invariant failed");
    }

    [Fact]
    public void Async_sequence_awaits_each_step()
    {
        Property property = Prop.ForAll((int[] values) => DbcPropertyTest.CheckSequenceAsync(
            () => Dbc.Make<IGauge>(new Gauge { Broken = true }),
            values.Select(value => (Func<IGauge, Task>)(g => g.SetAsync(value)))));

        AssertFalsified(property, "Postcondition failed: x.Level == value");
    }

    private static void AssertFalsified(Property property, string expected)
    {
        Exception failure = Assert.ThrowsAny<Exception>(() => Check.One(Strict, property));

        Assert.Contains("Falsifiable", failure.Message, StringComparison.Ordinal);
        Assert.Contains(expected, failure.Message, StringComparison.Ordinal);
    }

    [Property]
    public Property Property_attribute_integration(int value) =>
        DbcPropertyTest.Check(() => Dbc.Make<IGauge>(new Gauge()), g => g.Set(value));
}

[ContractClass(typeof(GaugeContracts))]
public interface IGauge
{
    int Level { get; }

    void Set(int value);

    void Rename(string name);

    void Crash();

    void Bump(bool bump);

    Task SetAsync(int value);

    ValueTask<int> ReadAsync(int value);
}

/// <summary>A level between 0 and 1000; <see cref="Broken"/> makes every write off by one.</summary>
public sealed class Gauge(int initialLevel = 0) : IGauge
{
    private int level = initialLevel;

    public bool Broken { get; init; }

    public int Level => level;

    public void Set(int value) => level = Broken ? value + 1 : value;

    public void Rename(string name)
    {
    }

    public void Crash() => throw new NotImplementedException();

    public void Bump(bool bump)
    {
        if (bump)
        {
            level += 400;
        }
    }

    public async Task SetAsync(int value)
    {
        await Task.Yield();
        Set(value);
    }

    public async ValueTask<int> ReadAsync(int value)
    {
        await Task.Yield();
        return Broken ? value + 1 : value;
    }
}

[ContractClassFor(typeof(IGauge))]
internal sealed class GaugeContracts
{
    public GaugeContracts()
    {
        Dbc.Def(static (IGauge x, int value) => x.Set(value),
            static (IGauge x, int value) => Contract.Requires(value >= 0 && value <= 1000),
            static (IGauge x, int value) => Contract.Ensures(x.Level == value));

        Dbc.Def(static (IGauge x, string name) => x.Rename(name),
            static (IGauge x, string name) => Contract.Requires<ArgumentNullException>(name != null, "name"));

        Dbc.Def(static (IGauge x, bool bump) => x.Bump(bump),
            static (IGauge x, bool bump) => Contract.Ensures(x.Level == Contract.OldValue(x.Level) + (bump ? 400 : 0)));

        Dbc.Def(static (IGauge x, int value) => x.SetAsync(value),
            static (IGauge x, int value) => Contract.Requires(value >= 0 && value <= 1000),
            static (IGauge x, int value) => Contract.Ensures(x.Level == value));

        Dbc.Def(static (IGauge x, int value) => x.ReadAsync(value),
            static (IGauge x, int value) => Contract.Ensures(Contract.Result<int>() == value));

        Dbc.Def(static (IGauge x) => Contract.Invariant(x.Level >= 0 && x.Level <= 1000));
    }
}
