#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Enforcement.Inheritance;

/// <summary>
/// Contract inheritance through interfaces (resolution R14): a proxy checks the contracts of its interface and of every
/// interface it extends, each loaded from its own <c>[ContractClass]</c>; sibling interfaces never share contracts.
/// Interface scenarios ported from CodeContracts Foxtrot/Tests/RewrittenInheritanceTest.cs.
/// </summary>
public class InheritanceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Base_interface_contracts_apply_to_a_derived_proxy_ported_from_InterfaceBaseRequires(bool mustBeTrue)
    {
        // Probe S17: neither source project applied IBase's contract class here.
        IDerived1 proxy = Dbc.Make<IDerived1>(new Implementation());

        if (mustBeTrue)
        {
            proxy.InterfaceBaseRequires(true);
            proxy.InterfaceBaseEnsures(true);
        }
        else
        {
            Expect.Violation(ContractFailureKind.Precondition, () => proxy.InterfaceBaseRequires(false));
            Expect.Violation(ContractFailureKind.Postcondition, () => proxy.InterfaceBaseEnsures(false));
        }
    }

    [Fact]
    public void Each_derived_interface_adds_its_own_contracts_ported_from_InterfaceRequires()
    {
        IDerived1 one = Dbc.Make<IDerived1>(new Implementation());
        IDerived2 two = Dbc.Make<IDerived2>(new Implementation());

        one.InterfaceRequires(4);
        Expect.Violation(ContractFailureKind.Precondition, () => one.InterfaceRequires(3));

        two.InterfaceRequires(3);
        Expect.Violation(ContractFailureKind.Precondition, () => two.InterfaceRequires(4));
    }

    [Fact]
    public void Interface_extending_both_gets_the_contracts_of_both_ported_from_MultipleInheritance()
    {
        IBoth proxy = Dbc.Make<IBoth>(new Implementation());

        ((IDerived1)proxy).InterfaceRequires(6);
        ((IDerived2)proxy).InterfaceRequires(6);
        Expect.Violation(ContractFailureKind.Precondition, () => ((IDerived1)proxy).InterfaceRequires(3));
        Expect.Violation(ContractFailureKind.Precondition, () => ((IDerived2)proxy).InterfaceRequires(4));
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.InterfaceBaseRequires(false));
    }

    [Fact]
    public void Derived_interface_can_strengthen_an_inherited_member()
    {
        IDerived1 derived = Dbc.Make<IDerived1>(new Implementation());
        IBase @base = Dbc.Make<IBase>(new Implementation());

        Expect.Violation(ContractFailureKind.Postcondition, () => derived.Value);
        Assert.Equal(-1, @base.Value);
    }

    [Fact]
    public void Contracts_registered_through_one_interface_do_not_leak_to_a_sibling()
    {
        // Probe S18: ContractExpressions applied IA's clause to IB because both share ISharedBase.Put.
        IA a = Dbc.Make<IA>(new Shared());
        IB b = Dbc.Make<IB>(new Shared());

        Expect.Violation(ContractFailureKind.Precondition, () => a.Put(-1));
        b.Put(-1);
    }

    [Fact]
    public void Inheritance_can_be_switched_off_with_ContractOption()
    {
        IIsolated proxy = Dbc.Make<IIsolated>(new Implementation());

        proxy.InterfaceBaseRequires(false);
    }
}

[ContractClass(typeof(BaseContracts))]
public interface IBase
{
    int Value { get; }

    void InterfaceBaseRequires(bool mustBeTrue);

    void InterfaceBaseEnsures(bool mustBeTrue);
}

[ContractClass(typeof(Derived1Contracts))]
public interface IDerived1 : IBase
{
    void InterfaceRequires(int value);

    int Strict(int value);
}

[ContractClass(typeof(Derived2Contracts))]
public interface IDerived2 : IBase
{
    void InterfaceRequires(int value);
}

public interface IBoth : IDerived1, IDerived2
{
}

[ContractOption("contract", "inheritance", false)]
public interface IIsolated : IBase
{
}

public sealed class Implementation : IBoth, IIsolated
{
    public int Value => -1;

    public void InterfaceBaseRequires(bool mustBeTrue)
    {
    }

    public void InterfaceBaseEnsures(bool mustBeTrue)
    {
    }

    public void InterfaceRequires(int value)
    {
    }

    public int Strict(int value) => value;
}

[ContractClassFor(typeof(IBase))]
internal sealed class BaseContracts
{
    public BaseContracts()
    {
        Dbc.Def(static (IBase x, bool mustBeTrue) => x.InterfaceBaseRequires(mustBeTrue),
            static (IBase x, bool mustBeTrue) => Contract.Requires(mustBeTrue));

        Dbc.Def(static (IBase x, bool mustBeTrue) => x.InterfaceBaseEnsures(mustBeTrue),
            static (IBase x, bool mustBeTrue) => Contract.Ensures(mustBeTrue));
    }
}

[ContractClassFor(typeof(IDerived1))]
internal sealed class Derived1Contracts
{
    public Derived1Contracts()
    {
        Dbc.Def(static (IDerived1 x, int value) => x.InterfaceRequires(value),
            static (IDerived1 x, int value) => Contract.Requires(value % 2 == 0));

        Dbc.Def(static (IDerived1 x, int value) => x.Strict(value),
            static (IDerived1 x, int value) => Contract.Requires(value >= 0));

        // Strengthens the inherited IBase.Value for IDerived1 proxies only.
        Dbc.Def(static (IDerived1 x) => x.Value,
            static (IDerived1 x) => Contract.Ensures(Contract.Result<int>() >= 0));
    }
}

[ContractClassFor(typeof(IDerived2))]
internal sealed class Derived2Contracts
{
    public Derived2Contracts()
    {
        Dbc.Def(static (IDerived2 x, int value) => x.InterfaceRequires(value),
            static (IDerived2 x, int value) => Contract.Requires(value % 3 == 0));
    }
}

public interface ISharedBase
{
    void Put(int value);
}

[ContractClass(typeof(AContracts))]
public interface IA : ISharedBase
{
}

public interface IB : ISharedBase
{
}

public sealed class Shared : IA, IB
{
    public void Put(int value)
    {
    }
}

[ContractClassFor(typeof(IA))]
internal sealed class AContracts
{
    public AContracts() =>
        Dbc.Def(static (IA x, int value) => x.Put(value), static (IA x, int value) => Contract.Requires(value >= 0));
}
