#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Enforcement.NonGeneric;

/// <summary><c>Dbc.Make(Type, object)</c> and <c>Dbc.HasContracts(Type)</c>, used where the interface is known only at run time (DI).</summary>
public class NonGenericMakeTests
{
    [Fact]
    public void Make_with_a_runtime_type_returns_a_checking_proxy()
    {
        var proxy = (IThermostat)Dbc.Make(typeof(IThermostat), new Thermostat());

        proxy.Set(20);
        Assert.Equal(20, proxy.Target);
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Set(99));
    }

    [Fact]
    public void Make_of_a_proxy_returns_it_unchanged()
    {
        object proxy = Dbc.Make(typeof(IThermostat), new Thermostat());

        Assert.Same(proxy, Dbc.Make(typeof(IThermostat), proxy));
    }

    [Fact]
    public void Creation_failures_are_not_wrapped() =>
        Expect.Violation(ContractFailureKind.Invariant, () => Dbc.Make(typeof(IThermostat), new Thermostat { Target = 99 }));

    [Fact]
    public void Arguments_are_validated()
    {
        Assert.Throws<ArgumentNullException>(() => Dbc.Make(null!, new Thermostat()));
        Assert.Throws<ArgumentNullException>(() => Dbc.Make(typeof(IThermostat), null!));
        Assert.Contains("not one", Assert.Throws<ArgumentException>(() => Dbc.Make(typeof(Thermostat), new Thermostat())).Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => Dbc.Make(typeof(IComparable<>), new Thermostat()));
        Assert.Contains("does not implement", Assert.Throws<ArgumentException>(() => Dbc.Make(typeof(IDisposable), new Thermostat())).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HasContracts_sees_contract_classes_inherited_contracts_and_direct_definitions()
    {
        Assert.True(Dbc.HasContracts(typeof(IThermostat)));
        Assert.True(Dbc.HasContracts(typeof(ISmartThermostat)));
        Assert.False(Dbc.HasContracts(typeof(IIsolatedThermostat)));
        Assert.False(Dbc.HasContracts(typeof(IUnchecked)));

        Assert.False(Dbc.HasContracts(typeof(IDirect)));
        Dbc.Def(static (IDirect x, int v) => x.Apply(v), static (IDirect x, int v) => Contract.Requires(v > 0));
        Assert.True(Dbc.HasContracts(typeof(IDirect)));
    }

    [Fact]
    public void HasContracts_is_false_for_types_that_cannot_have_contracts()
    {
        Assert.False(Dbc.HasContracts(typeof(Thermostat)));
        Assert.False(Dbc.HasContracts(typeof(IComparable<>)));
        Assert.Throws<ArgumentNullException>(() => Dbc.HasContracts(null!));
    }
}

[ContractClass(typeof(ThermostatContracts))]
public interface IThermostat
{
    int Target { get; }

    void Set(int target);
}

public interface ISmartThermostat : IThermostat
{
    void Learn();
}

[ContractOption("contract", "inheritance", false)]
public interface IIsolatedThermostat : IThermostat
{
}

public interface IUnchecked
{
    void Run();
}

public interface IDirect
{
    void Apply(int value);
}

public sealed class Thermostat : ISmartThermostat, IIsolatedThermostat
{
    public int Target { get; set; } = 18;

    public void Set(int target) => Target = target;

    public void Learn()
    {
    }
}

[ContractClassFor(typeof(IThermostat))]
internal sealed class ThermostatContracts
{
    public ThermostatContracts()
    {
        Dbc.Def(static (IThermostat x, int target) => x.Set(target), static (IThermostat x, int target) => Contract.Requires(target >= 5 && target <= 30));
        Dbc.Def(static (IThermostat x) => Contract.Invariant(x.Target >= 5 && x.Target <= 30));
    }
}
