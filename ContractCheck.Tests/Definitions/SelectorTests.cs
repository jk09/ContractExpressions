#define CONTRACTS_FULL

using System.Collections;
using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Definitions.Selectors;

/// <summary>
/// Which interface member a <c>Dbc.Def</c> selector attaches clauses to, and which selectors are rejected (resolution R13:
/// the outermost call, validated to be a member of the interface).
/// </summary>
public class SelectorTests
{
    [Fact]
    public void Method_selector_attaches_clauses_to_that_method()
    {
        ISelectable proxy = Dbc.Make<ISelectable>(new Selectable());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Twice(-1));
        Assert.Equal(4, proxy.Twice(2));
    }

    [Fact]
    public void Property_selector_attaches_clauses_to_the_getter()
    {
        var target = new Selectable { Level = -1 };

        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<ISelectable>(target).Level);
    }

    [Fact]
    public void Indexer_selector_attaches_clauses_to_the_indexer_getter()
    {
        ISelectable proxy = Dbc.Make<ISelectable>(new Selectable());

        Assert.Equal("item 3", proxy[3]);
        Expect.Violation(ContractFailureKind.Precondition, () => proxy[-3]);
    }

    [Fact]
    public void Selector_of_an_inherited_member_attaches_clauses_for_the_derived_interface()
    {
        // IList.Add is declared by IList; the clause is registered for IItems (the selector's interface).
        IItems proxy = Dbc.Make<IItems>(new Items());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Add(null));
        Assert.Equal(0, proxy.Add("a"));
    }

    [Fact]
    public void Clauses_registered_for_a_derived_interface_do_not_apply_to_its_base_interface()
    {
        IList list = Dbc.Make<IList>(new Items());

        Assert.Equal(0, list.Add(null));
    }

    [Fact]
    public void Generic_method_clauses_apply_to_the_selected_instantiation_only()
    {
        ISelectable proxy = Dbc.Make<ISelectable>(new Selectable());

        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Echo<int>(-1));
        Assert.Equal(-1L, proxy.Echo<long>(-1L));
        Assert.Equal(5, proxy.Echo(5));
    }

    [Fact]
    public void Nested_member_access_in_the_selector_is_rejected_instead_of_selecting_the_inner_member()
    {
        // ContractExpressions attached these clauses to get_Count (the last member it visited) and then crashed
        // calling it (probe S36). ContractCheck requires the selector to pass its own parameters.
        Expect.DefinitionError(
            () => Dbc.Def(static (ISelectable x, int v) => x.Twice(x.Level), static (ISelectable x, int v) => Contract.Requires(v > 100)),
            "pass the selector's own parameters");
    }

    [Fact]
    public void Arguments_must_be_the_selector_parameters_in_order()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (ISelectable x, int a, int b) => x.Subtract(b, a), static (ISelectable x, int a, int b) => Contract.Requires(a > b)),
            "in declaration order");
    }

    [Fact]
    public void Constant_arguments_are_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (ISelectable x) => x.Twice(5), static (ISelectable x) => Contract.Ensures(Contract.Result<int>() == 10)),
            "pass the selector's own parameters");
    }

    [Fact]
    public void Member_of_another_object_is_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (ISelectable x) => x.Child.Twice(1), static (ISelectable x) => Contract.Requires(true)),
            "must be invoked on the selector's first parameter");
    }

    [Fact]
    public void Static_and_extension_methods_are_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (ISelectable x) => Helper(x), static (ISelectable x) => Contract.Requires(true)),
            "must be invoked on the selector's first parameter");
        Expect.DefinitionError(
            () => Dbc.Def(static (IItems x) => x.Cast<object>(), static (IItems x) => Contract.Requires(true)),
            "must be invoked on the selector's first parameter");
    }

    [Fact]
    public void Members_of_object_are_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (ISelectable x) => x.ToString(), static (ISelectable x) => Contract.Requires(true)),
            "is not a member of ISelectable");
    }

    [Fact]
    public void Contracts_for_classes_are_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (Selectable x, int v) => x.Twice(v), static (Selectable x, int v) => Contract.Requires(v > 0)),
            "contracts can only be defined for interfaces");
    }

    [Fact]
    public void DefSetter_needs_a_property_with_a_setter()
    {
        Expect.DefinitionError(
            () => Dbc.DefSetter(static (ISelectable x, int v) => x.Twice(v), static (ISelectable x, int v, int value) => Contract.Requires(true)),
            "needs a property or an indexer");
        Expect.DefinitionError(
            () => Dbc.DefSetter(static (ISelectable x) => x.Level, static (ISelectable x, int value) => Contract.Requires(true)),
            "has no setter");
    }

    [Fact]
    public void A_selector_without_clauses_registers_nothing()
    {
        // ContractExpressions accepted this (no-op); ContractExpressions4 threw. ContractCheck keeps the no-op (R16).
        Dbc.Def(static (ISelectable x, int a, int b) => x.Subtract(a, b));

        Assert.Equal(-1, Dbc.Make<ISelectable>(new Selectable()).Subtract(1, 2));
    }

    [Fact]
    public void Null_arguments_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => Dbc.Def<ISelectable>(null!, static (ISelectable x) => Contract.Requires(true)));
        Assert.Throws<ArgumentNullException>(() => Dbc.Def(static (ISelectable x) => x.Reset(), null!));
        Assert.Throws<ArgumentNullException>(() => Dbc.Def(static (ISelectable x) => x.Reset(), [null!]));
    }

    private static int Helper(ISelectable selectable) => selectable.Level;
}

[ContractClass(typeof(SelectableContracts))]
public interface ISelectable
{
    int Level { get; }

    ISelectable Child { get; }

    string this[int index] { get; }

    int Twice(int value);

    int Subtract(int a, int b);

    T Echo<T>(T value);

    void Reset();
}

public sealed class Selectable : ISelectable
{
    public int Level { get; set; }

    public ISelectable Child => this;

    public string this[int index] => "item " + index;

    public int Twice(int value) => value * 2;

    public int Subtract(int a, int b) => a - b;

    public T Echo<T>(T value) => value;

    public void Reset() => Level = 0;
}

[ContractClassFor(typeof(ISelectable))]
internal sealed class SelectableContracts
{
    public SelectableContracts()
    {
        Dbc.Def(static (ISelectable x, int value) => x.Twice(value),
            static (ISelectable x, int value) => Contract.Requires(value >= 0));

        Dbc.Def(static (ISelectable x) => x.Level,
            static (ISelectable x) => Contract.Ensures(Contract.Result<int>() >= 0));

        Dbc.Def(static (ISelectable x, int index) => x[index],
            static (ISelectable x, int index) => Contract.Requires(index >= 0));

        Dbc.Def(static (ISelectable x, int value) => x.Echo(value),
            static (ISelectable x, int value) => Contract.Requires(value >= 0));
    }
}

[ContractClass(typeof(ItemsContracts))]
public interface IItems : IList
{
}

public sealed class Items : ArrayList, IItems
{
}

[ContractClassFor(typeof(IItems))]
internal sealed class ItemsContracts
{
    public ItemsContracts()
    {
        Dbc.Def(static (IItems x, object? value) => x.Add(value),
            static (IItems x, object? value) => Contract.Requires(value != null),
            static (IItems x, object? value) => Contract.Ensures(Contract.Result<int>() == x.Count - 1));
    }
}
