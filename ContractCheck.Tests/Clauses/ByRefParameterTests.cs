#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Clauses.ByRef;

/// <summary>
/// Contracts on <see langword="out"/>/<see langword="ref"/> parameters through the <see cref="Dbc.Out{T}"/> and
/// <see cref="Dbc.Ref{T}"/> placeholders (expression-tree lambdas can't declare by-ref parameters), and
/// <c>Contract.ValueAtReturn</c>, which CodeContracts used for out parameters.
/// </summary>
public class ByRefParameterTests
{
    [Fact]
    public void Out_values_reach_the_caller_and_postconditions_check_them()
    {
        IStore proxy = Dbc.Make<IStore>(new Store());

        Assert.True(proxy.TryGet("a", out int found));
        Assert.Equal(1, found);
        Assert.False(proxy.TryGet("missing", out int missing));
        Assert.Equal(0, missing);

        ContractViolationException exception = Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IStore>(new Store { Broken = true }).TryGet("a", out _));
        Assert.Equal("!Contract.Result<bool>() || Dbc.Out<int>.Value >= 0", exception.Condition);
    }

    [Fact]
    public void ValueAtReturn_of_a_placeholder_is_the_value_at_return()
    {
        ContractViolationException exception = Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IStore>(new Store { Broken = true }).TryGet("missing", out _));

        Assert.Equal("Contract.Result<bool>() || Contract.ValueAtReturn(out Dbc.Out<int>.Value) == 0", exception.Condition);
    }

    [Fact]
    public void Ref_placeholders_are_the_value_passed_in_for_entry_checks_and_the_value_at_return_afterwards()
    {
        IStore proxy = Dbc.Make<IStore>(new Store());
        int counter = 5;

        proxy.Bump(ref counter, 2);

        Assert.Equal(7, counter);
        int negative = -1;
        Assert.Equal("the counter must not be negative", Expect.Violation(ContractFailureKind.Precondition, () => proxy.Bump(ref negative, 2)).UserMessage);
        int other = 1;
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IStore>(new Store { Broken = true }).Bump(ref other, 2));
    }

    [Fact]
    public void Lambda_parameters_after_a_by_ref_argument_map_to_their_own_argument()
    {
        IStore proxy = Dbc.Make<IStore>(new Store());
        int counter = 0;

        // step is the selector's second parameter but the member's second argument too: (ref counter, step).
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Bump(ref counter, 0));
    }

    [Fact]
    public void Several_out_parameters_of_one_type_use_Value_and_Value2()
    {
        IStore proxy = Dbc.Make<IStore>(new Store());

        Assert.True(proxy.Split("left,right", out string head, out string tail));
        Assert.Equal(("left", "right"), (head, tail));
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IStore>(new Store { Broken = true }).Split("left,right", out _, out _));
    }

    [Fact]
    public void Several_ref_parameters_of_one_type_compare_with_their_old_values()
    {
        IStore proxy = Dbc.Make<IStore>(new Store());
        int a = 1, b = 2;

        proxy.Swap(ref a, ref b);

        Assert.Equal((2, 1), (a, b));
        int c = 1, d = 2;
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IStore>(new Store { Broken = true }).Swap(ref c, ref d));
    }

    [Fact]
    public void In_parameters_take_the_selector_parameter()
    {
        IStore proxy = Dbc.Make<IStore>(new Store());

        Assert.Equal(4, proxy.Echo(4));
        Expect.Violation(ContractFailureKind.Precondition, () => proxy.Echo(-1));
        Expect.Violation(ContractFailureKind.Postcondition, () => Dbc.Make<IStore>(new Store { Broken = true }).Echo(4));
    }

    [Fact]
    public void An_out_parameter_needs_an_Out_placeholder() =>
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s, string key) => s.TryGet(key, out Dbc.Ref<int>.Value)),
            "'value' is an out parameter, so pass Dbc.Out<int>.Value instead of Dbc.Ref<int>.Value");

    [Fact]
    public void A_ref_parameter_needs_a_Ref_placeholder() =>
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s, int step) => s.Bump(ref Dbc.Out<int>.Value, step)),
            "'counter' is a ref parameter, so pass Dbc.Ref<int>.Value instead of Dbc.Out<int>.Value");

    [Fact]
    public void A_by_ref_parameter_needs_a_placeholder_not_another_field() =>
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s, string key) => s.TryGet(key, out NotAPlaceholder.Value)),
            "pass Dbc.Out<int>.Value for the out parameter 'value'");

    [Fact]
    public void A_placeholder_can_stand_for_one_parameter_only() =>
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s) => s.Swap(ref Dbc.Ref<int>.Value, ref Dbc.Ref<int>.Value)),
            "Dbc.Ref<int>.Value is passed for two parameters; use Value, Value2 and Value3");

    [Fact]
    public void Clauses_can_only_use_placeholders_the_selector_passes() =>
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s, string key) => s.TryGet(key, out Dbc.Out<int>.Value),
                static (IStore s, string key) => Contract.Ensures(Dbc.Out<int>.Value2 >= 0)),
            "the selector doesn't pass Dbc.Out<int>.Value2");

    [Fact]
    public void Out_placeholders_have_no_value_in_entry_checks() =>
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s, string key) => s.TryGet(key, out Dbc.Out<int>.Value),
                static (IStore s, string key) => Contract.Requires(Dbc.Out<int>.Value == 0)),
            "out parameter 'value' has no value before the call");

    [Fact]
    public void Out_placeholders_have_no_value_when_the_member_throws() =>
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s, string key) => s.TryGet(key, out Dbc.Out<int>.Value),
                static (IStore s, string key) => Contract.EnsuresOnThrow<InvalidOperationException>(Dbc.Out<int>.Value == 0)),
            "out parameter 'value' has no defined value when the member throws");

    [Fact]
    public void Old_values_of_out_parameters_do_not_exist() =>
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s, string key) => s.TryGet(key, out Dbc.Out<int>.Value),
                static (IStore s, string key) => Contract.Ensures(Contract.OldValue(Dbc.Out<int>.Value) == 0)),
            "out parameter 'value' has no value on entry");

    [Fact]
    public void Ref_values_at_a_throw_are_unknown_but_their_old_values_are_not()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s, int step) => s.Bump(ref Dbc.Ref<int>.Value, step),
                static (IStore s, int step) => Contract.EnsuresOnThrow<InvalidOperationException>(Dbc.Ref<int>.Value >= 0)),
            "the value of ref parameter 'counter' when the member throws is unknown; use Contract.OldValue(Dbc.Ref<int>.Value)");

        Dbc.Def(static (IUnrelatedStore s, int step) => s.Bump(ref Dbc.Ref<int>.Value, step),
            static (IUnrelatedStore s, int step) => Contract.EnsuresOnThrow<InvalidOperationException>(Contract.OldValue(Dbc.Ref<int>.Value) >= 0));
    }

    [Fact]
    public void ValueAtReturn_is_for_Ensures_only() =>
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s, int step) => s.Bump(ref Dbc.Ref<int>.Value, step),
                static (IStore s, int step) => Contract.Requires(Contract.ValueAtReturn(out Dbc.Ref<int>.Value) >= 0)),
            "Contract.ValueAtReturn can only be used in Ensures clauses");

    [Fact]
    public void Interface_invariants_have_no_placeholders() =>
        Expect.DefinitionError(
            () => Dbc.Def(static (IStore s) => Contract.Invariant(Dbc.Ref<int>.Value >= 0)),
            "interface invariants have none");
}

[ContractClass(typeof(StoreContracts))]
public interface IStore
{
    bool TryGet(string key, out int value);

    void Bump(ref int counter, int step);

    bool Split(string text, out string head, out string tail);

    void Swap(ref int first, ref int second);

    int Echo(in int value);
}

public interface IUnrelatedStore
{
    void Bump(ref int counter, int step);
}

/// <summary>With <see cref="Broken"/>, every member returns or writes a value its contract rules out.</summary>
public sealed class Store : IStore
{
    private readonly Dictionary<string, int> values = new() { ["a"] = 1 };

    public bool Broken { get; init; }

    public bool TryGet(string key, out int value)
    {
        bool found = values.TryGetValue(key, out value);
        if (Broken)
        {
            value = -1;
        }

        return found;
    }

    public void Bump(ref int counter, int step) => counter += Broken ? step + 1 : step;

    public bool Split(string text, out string head, out string tail)
    {
        int comma = text.IndexOf(',', StringComparison.Ordinal);
        if (comma < 0)
        {
            head = tail = string.Empty;
            return false;
        }

        head = text[..comma];
        tail = Broken ? string.Empty : text[(comma + 1)..];
        return true;
    }

    public void Swap(ref int first, ref int second)
    {
        if (!Broken)
        {
            (first, second) = (second, first);
        }
    }

    public int Echo(in int value) => Broken ? value + 1 : value;
}

public static class NotAPlaceholder
{
#pragma warning disable CA2211
    public static int Value;
#pragma warning restore CA2211
}

[ContractClassFor(typeof(IStore))]
internal sealed class StoreContracts
{
    public StoreContracts()
    {
        Dbc.Def(static (IStore s, string key) => s.TryGet(key, out Dbc.Out<int>.Value),
            static (IStore s, string key) => Contract.Requires(key != null),
            static (IStore s, string key) => Contract.Ensures(!Contract.Result<bool>() || Dbc.Out<int>.Value >= 0),
            static (IStore s, string key) => Contract.Ensures(Contract.Result<bool>() || Contract.ValueAtReturn(out Dbc.Out<int>.Value) == 0));

        Dbc.Def(static (IStore s, int step) => s.Bump(ref Dbc.Ref<int>.Value, step),
            static (IStore s, int step) => Contract.Requires(Dbc.Ref<int>.Value >= 0, "the counter must not be negative"),
            static (IStore s, int step) => Contract.Requires(step > 0),
            static (IStore s, int step) => Contract.Ensures(Dbc.Ref<int>.Value == Contract.OldValue(Dbc.Ref<int>.Value) + step));

        Dbc.Def(static (IStore s, string text) => s.Split(text, out Dbc.Out<string>.Value, out Dbc.Out<string>.Value2),
            static (IStore s, string text) => Contract.Ensures(!Contract.Result<bool>() || Dbc.Out<string>.Value + "," + Dbc.Out<string>.Value2 == text));

        Dbc.Def(static (IStore s) => s.Swap(ref Dbc.Ref<int>.Value, ref Dbc.Ref<int>.Value2),
            static (IStore s) => Contract.Ensures(Dbc.Ref<int>.Value == Contract.OldValue(Dbc.Ref<int>.Value2) && Dbc.Ref<int>.Value2 == Contract.OldValue(Dbc.Ref<int>.Value)));

        Dbc.Def(static (IStore s, int value) => s.Echo(value),
            static (IStore s, int value) => Contract.Requires(value >= 0),
            static (IStore s, int value) => Contract.Ensures(Contract.Result<int>() == value));
    }
}
