using Microsoft.CodeAnalysis;
using static ContractCheck.Tests.Analyzers.AnalyzerHarness;
using static ContractCheck.Tests.Analyzers.Snippets;

namespace ContractCheck.Tests.Analyzers;

/// <summary>CCK0001–CCK0005 and CCK0008: the Dbc.Def rules of the run time, reported at compile time.</summary>
public sealed class ContractDefinitionAnalyzerTests
{
    // CCK0001 – selectors

    [Fact]
    public Task CCK0001_reports_a_contract_type_that_is_not_an_interface() => ReportsAsync(
        "CCK0001",
        Define("Dbc.Def([|(string s) => s.Length|]);"),
        "contracts can only be defined for interfaces", "string is not one");

    [Fact]
    public Task CCK0001_reports_arguments_out_of_order() => ReportsAsync(
        "CCK0001",
        Define("Dbc.Def((IStore x, int start, string item) => [|x.Find(item, start)|]);"),
        "pass the selector's own parameters, in declaration order");

    [Fact]
    public Task CCK0001_reports_a_member_of_another_parameter() => ReportsAsync(
        "CCK0001",
        Define("Dbc.Def((IStore x, IStore y) => [|y.Count|]);"),
        "'Count' must be invoked on the selector's first parameter 'x'");

    [Fact]
    public Task CCK0001_reports_object_members() => ReportsAsync(
        "CCK0001",
        Define("Dbc.Def((IStore x) => [|x.GetHashCode()|]);"),
        "'GetHashCode' is not a member of IStore or its base interfaces");

    [Fact]
    public Task CCK0001_reports_DefSetter_on_a_read_only_property() => ReportsAsync(
        "CCK0001",
        Define("Dbc.DefSetter((IStore x) => [|x.Count|]);"),
        "property 'Count' has no setter");

    [Fact]
    public Task CCK0001_reports_an_out_parameter_without_placeholder() => ReportsAsync(
        "CCK0001",
        Define("Dbc.Def((IStore x, string key) => x.TryGet(key, [|out Holder.Field|]));"),
        "pass Dbc.Out<int>.Value for the out parameter 'value'");

    [Fact]
    public Task CCK0001_reports_a_placeholder_of_the_wrong_kind() => ReportsAsync(
        "CCK0001",
        Define("Dbc.Def((IStore x, string key) => x.TryGet(key, [|out Dbc.Ref<int>.Value|]));"),
        "'value' is an out parameter, so pass Dbc.Out<int>.Value instead of Dbc.Ref<int>.Value");

    [Fact]
    public Task CCK0001_is_silent_on_valid_selectors() => SilentAsync(Define("""
        Dbc.Def((IStore x, string item, int start) => x.Find(item, start), (IStore x, string item, int start) => Contract.Requires(start >= 0));
        Dbc.Def((IStore x) => x.Count, (IStore x) => Contract.Ensures(Contract.Result<int>() >= 0));
        Dbc.Def((IStore x, int index) => x[index], (IStore x, int index) => Contract.Requires(index >= 0));
        Dbc.DefSetter((IStore x) => x.Limit, (IStore x, int value) => Contract.Requires(value > 0));
        Dbc.DefSetter((IStore x, int index) => x[index], (IStore x, int index, string value) => Contract.Requires(value != null));
        Dbc.Def((IStore x, string key) => x.TryGet(key, out Dbc.Out<int>.Value));
        Dbc.Def((IStore x) => x.Bump(ref Dbc.Ref<int>.Value));
        Dbc.Def((ISpecialStore x) => ((IStore)x).Count);
        """));

    [Fact]
    public Task CCK0001_leaves_contract_types_that_are_type_parameters_to_the_run_time() => SilentAsync(Prelude + """
        public static class Generic
        {
            public static void Define<T>() where T : class => Dbc.Def((T x) => x.ToString());
        }
        """);

    // CCK0002 – clauses

    [Fact]
    public Task CCK0002_reports_a_clause_that_is_not_a_Contract_call() => ReportsAsync(
        "CCK0002",
        Define("Dbc.Def((IStore x, string item) => x.Add(item), (IStore x, string item) => [|Console.WriteLine(item)|]);"),
        "a contract clause must be a single call to System.Diagnostics.Contracts.Contract");

    [Fact]
    public Task CCK0002_reports_a_Contract_method_that_is_not_a_clause() => ReportsAsync(
        "CCK0002",
        Define("Dbc.Def((IStore x) => x.Items, (IStore x) => [|Contract.ForAll(x.Items, s => s != null)|]);"),
        "Contract.ForAll is not a clause");

    [Fact]
    public Task CCK0002_reports_a_precondition_in_the_invariant_form() => ReportsAsync(
        "CCK0002",
        Define("Dbc.Def((IStore x) => [|Contract.Requires(x.Count >= 0)|]);"),
        "a Dbc.Def without a member selector can only define invariants; attach Contract.Requires to a member");

    [Fact]
    public Task CCK0002_reports_a_non_invariant_clause_after_an_invariant() => ReportsAsync(
        "CCK0002",
        Define("Dbc.Def((IStore x) => Contract.Invariant(x.Count >= 0), (IStore x) => [|Contract.Ensures(x.Count >= 0)|]);"),
        "an interface-level Dbc.Def accepts only Contract.Invariant clauses");

    [Fact]
    public Task CCK0002_reports_a_typed_precondition_with_an_abstract_exception() => ReportsAsync(
        "CCK0002",
        Define("Dbc.Def((IStore x, string item) => x.Add(item), (IStore x, string item) => [|Contract.Requires<AbstractFailure>(item != null)|]);"),
        "Contract.Requires<AbstractFailure> cannot be thrown: 'AbstractFailure' is abstract");

    [Fact]
    public Task CCK0002_reports_a_typed_precondition_without_a_usable_constructor() => ReportsAsync(
        "CCK0002",
        Define("Dbc.Def((IStore x, string item) => x.Add(item), (IStore x, string item) => [|Contract.Requires<NoMessageFailure>(item != null)|]);"),
        "'NoMessageFailure' has no public (string paramName, string message), (string message) or parameterless constructor");

    [Fact]
    public Task CCK0002_is_silent_on_valid_clauses() => SilentAsync(Define("""
        Dbc.Def((IStore x) => Contract.Invariant(x.Count >= 0), (IStore x) => Contract.Invariant(x.Count <= x.Limit, "within the limit"));
        Dbc.Def(
            (IStore x, string item) => x.Add(item),
            (IStore x, string item) => Contract.Requires<ArgumentNullException>(item != null, "item"),
            (IStore x, string item) => Contract.Requires<InvalidOperationException>(x.Count < x.Limit),
            (IStore x, string item) => Contract.Assert(x.Count >= 0),
            (IStore x, string item) => Contract.Assume(x.Limit > 0),
            (IStore x, string item) => Contract.Ensures(x.Count > 0),
            (IStore x, string item) => Contract.EnsuresOnThrow<InvalidOperationException>(x.Count == Contract.OldValue(x.Count)),
            (IStore x, string item) => Contract.Invariant(x.Count >= 0));
        """));

    // CCK0003 – Contract.Result

    [Fact]
    public Task CCK0003_reports_Result_in_a_precondition() => ReportsAsync(
        "CCK0003",
        Define("Dbc.Def((IStore x) => x.Count, (IStore x) => Contract.Requires([|Contract.Result<int>()|] >= 0));"),
        "Contract.Result can only be used in Ensures clauses");

    [Fact]
    public Task CCK0003_reports_Result_in_EnsuresOnThrow() => ReportsAsync(
        "CCK0003",
        Define("Dbc.Def((IStore x) => x.Count, (IStore x) => Contract.EnsuresOnThrow<Exception>([|Contract.Result<int>()|] >= 0));"),
        "Contract.Result cannot be used in EnsuresOnThrow");

    [Fact]
    public Task CCK0003_reports_Result_of_a_void_member() => ReportsAsync(
        "CCK0003",
        Define("Dbc.Def((IStore x, string item) => x.Add(item), (IStore x, string item) => Contract.Ensures([|Contract.Result<int>()|] > 0));"),
        "IStore.Add returns void, so Contract.Result<int>() has no value");

    [Fact]
    public Task CCK0003_reports_Result_of_the_wrong_type() => ReportsAsync(
        "CCK0003",
        Define("Dbc.Def((IStore x) => x.CountAsync(), (IStore x) => Contract.Ensures([|Contract.Result<long>()|] >= 0));"),
        "Contract.Result<long>() does not match the return type of IStore.CountAsync; use Contract.Result<int (the awaited result) or Task<int>>()");

    [Fact]
    public Task CCK0003_reports_a_clause_reading_both_the_task_and_its_result() => ReportsAsync(
        "CCK0003",
        Define("Dbc.Def((IStore x) => x.CountAsync(), (IStore x) => [|Contract.Ensures(Contract.Result<Task<int>>() != null && Contract.Result<int>() >= 0)|]);"),
        "a clause cannot read both the returned task and its awaited result");

    [Fact]
    public Task CCK0003_is_silent_on_valid_results() => SilentAsync(Define("""
        Dbc.Def((IStore x) => x.Count, (IStore x) => Contract.Ensures(Contract.Result<int>() >= 0, "never negative"));
        Dbc.Def((IStore x) => x.CountAsync(), (IStore x) => Contract.Ensures(Contract.Result<int>() >= 0));
        Dbc.Def((IStore x) => x.CountAsync(), (IStore x) => Contract.Ensures(Contract.Result<Task<int>>() != null));
        Dbc.Def((IStore x, string item, int start) => x.Find(item, start), (IStore x, string item, int start) => Contract.Ensures(Contract.Result<int>() >= -1));
        """));

    // CCK0004 – Contract.OldValue

    [Fact]
    public Task CCK0004_reports_OldValue_in_a_precondition() => ReportsAsync(
        "CCK0004",
        Define("Dbc.Def((IStore x, string item) => x.Add(item), (IStore x, string item) => Contract.Requires([|Contract.OldValue(x.Count)|] >= 0));"),
        "Contract.OldValue can only be used in Ensures and EnsuresOnThrow clauses");

    [Fact]
    public Task CCK0004_reports_OldValue_of_a_literal() => ReportsAsync(
        "CCK0004",
        Define("Dbc.Def((IStore x, string item) => x.Add(item), (IStore x, string item) => Contract.Ensures(x.Count > [|Contract.OldValue(0)|]));"),
        "Contract.OldValue of a literal is always the literal");

    [Fact]
    public Task CCK0004_reports_OldValue_of_the_result() => ReportsAsync(
        "CCK0004",
        Define("Dbc.Def((IStore x) => x.Count, (IStore x) => Contract.Ensures([|Contract.OldValue(Contract.Result<int>())|] >= 0));"),
        "Contract.OldValue cannot contain Contract.Result or another Contract.OldValue");

    [Fact]
    public Task CCK0004_reports_OldValue_of_a_lambda_variable() => ReportsAsync(
        "CCK0004",
        Define("Dbc.Def((IStore x, string item) => x.Add(item), (IStore x, string item) => Contract.Ensures(Contract.ForAll(x.Items, s => s == [|Contract.OldValue(s)|])));"),
        "Contract.OldValue can only refer to the variable of an enclosing Contract.ForAll/Exists(from, to, i => ...)");

    [Fact]
    public Task CCK0004_is_silent_on_valid_old_values() => SilentAsync(Define("""
        Dbc.Def(
            (IStore x, string item) => x.Add(item),
            (IStore x, string item) => Contract.Ensures(x.Count == Contract.OldValue(x.Count) + 1),
            (IStore x, string item) => Contract.Ensures(Contract.ForAll(0, x.Count - 1, i => x[i] == Contract.OldValue(x[i]))),
            (IStore x, string item) => Contract.Ensures(Contract.ForAll(0, Contract.OldValue(x.Count), i => x[i] != null)),
            (IStore x, string item) => Contract.Ensures(Contract.Exists(0, x.Count, i => x[i] == item)),
            (IStore x, string item) => Contract.Ensures(Contract.OldValue(x.Items.Where(s => s != null).ToArray()).Length <= x.Count),
            (IStore x, string item) => Contract.EnsuresOnThrow<InvalidOperationException>(x.Count == Contract.OldValue(x.Count)));
        """));

    // CCK0005 – out/ref values, ValueAtReturn, EndContractBlock

    [Fact]
    public Task CCK0005_reports_an_out_value_in_a_precondition() => ReportsAsync(
        "CCK0005",
        Define("Dbc.Def((IStore x, string key) => x.TryGet(key, out Dbc.Out<int>.Value), (IStore x, string key) => Contract.Requires([|Dbc.Out<int>.Value|] >= 0));"),
        "out parameter 'value' has no value before the call; read it in an Ensures clause");

    [Fact]
    public Task CCK0005_reports_a_placeholder_the_selector_does_not_pass() => ReportsAsync(
        "CCK0005",
        Define("Dbc.Def((IStore x, string key) => x.TryGet(key, out Dbc.Out<int>.Value), (IStore x, string key) => Contract.Ensures([|Dbc.Out<int>.Value2|] >= 0));"),
        "the selector doesn't pass Dbc.Out<int>.Value2 for a parameter of IStore.TryGet");

    [Fact]
    public Task CCK0005_reports_OldValue_of_an_out_value() => ReportsAsync(
        "CCK0005",
        Define("Dbc.Def((IStore x, string key) => x.TryGet(key, out Dbc.Out<int>.Value), (IStore x, string key) => Contract.Ensures(Contract.OldValue([|Dbc.Out<int>.Value|]) <= 0));"),
        "out parameter 'value' has no value on entry, so Contract.OldValue can't capture it");

    [Fact]
    public Task CCK0005_reports_a_ref_value_in_EnsuresOnThrow() => ReportsAsync(
        "CCK0005",
        Define("Dbc.Def((IStore x) => x.Bump(ref Dbc.Ref<int>.Value), (IStore x) => Contract.EnsuresOnThrow<Exception>([|Dbc.Ref<int>.Value|] >= 0));"),
        "the value of ref parameter 'counter' when the member throws is unknown; use Contract.OldValue(Dbc.Ref<int>.Value)");

    [Fact]
    public Task CCK0005_reports_ValueAtReturn_without_a_placeholder() => ReportsAsync(
        "CCK0005",
        Define("Dbc.Def((IStore x) => x.Count, (IStore x) => Contract.Ensures([|Contract.ValueAtReturn(out Holder.Field)|] >= 0));"),
        "Contract.ValueAtReturn needs an out/ref placeholder");

    [Fact]
    public Task CCK0005_reports_ValueAtReturn_in_a_precondition() => ReportsAsync(
        "CCK0005",
        Define("Dbc.Def((IStore x) => x.Bump(ref Dbc.Ref<int>.Value), (IStore x) => Contract.Requires([|Contract.ValueAtReturn(out Dbc.Ref<int>.Value)|] >= 0));"),
        "Contract.ValueAtReturn can only be used in Ensures clauses");

    [Fact]
    public Task CCK0005_reports_EndContractBlock_as_a_clause() => ReportsAsync(
        "CCK0005",
        Define("Dbc.Def((IStore x) => x.Count, (IStore x) => [|Contract.EndContractBlock()|]);"),
        "Contract.EndContractBlock marks legacy if-then-throw preconditions");

    [Fact]
    public Task CCK0005_is_silent_on_valid_out_and_ref_values() => SilentAsync(Define("""
        Dbc.Def(
            (IStore x, string key) => x.TryGet(key, out Dbc.Out<int>.Value),
            (IStore x, string key) => Contract.Requires(key != null),
            (IStore x, string key) => Contract.Ensures(!Contract.Result<bool>() || Dbc.Out<int>.Value >= 0),
            (IStore x, string key) => Contract.Ensures(Contract.ValueAtReturn(out Dbc.Out<int>.Value) >= 0));
        Dbc.Def(
            (IStore x) => x.Bump(ref Dbc.Ref<int>.Value),
            (IStore x) => Contract.Requires(Dbc.Ref<int>.Value >= 0),
            (IStore x) => Contract.Ensures(Dbc.Ref<int>.Value == Contract.OldValue(Dbc.Ref<int>.Value) + 1),
            (IStore x) => Contract.EnsuresOnThrow<OverflowException>(Contract.OldValue(Dbc.Ref<int>.Value) == int.MaxValue));
        """));

    // CCK0008 – preconditions added through a derived interface

    [Fact]
    public Task CCK0008_reports_a_precondition_on_an_inherited_member() => ReportsAsync(
        "CCK0008",
        Define("Dbc.Def((ISpecialStore x, string item) => x.Add(item), (ISpecialStore x, string item) => [|Contract.Requires(item != null)|]);"),
        "Contract.Requires adds a precondition to IStore.Add through ISpecialStore");

    [Fact]
    public async Task CCK0008_reports_every_entry_check_kind_and_keeps_the_definition_valid()
    {
        Diagnostic diagnostic = await ReportsAsync(
            "CCK0008",
            Define("Dbc.Def((ISpecialStore x) => x.Count, (ISpecialStore x) => [|Contract.Assume(x.Limit > 0)|]);"),
            "Contract.Assume adds a precondition to IStore.Count through ISpecialStore");
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Fact]
    public Task CCK0008_is_silent_on_own_members_and_on_postconditions() => SilentAsync(Define("""
        Dbc.Def((IStore x, string item) => x.Add(item), (IStore x, string item) => Contract.Requires(item != null));
        Dbc.Def((ISpecialStore x) => x.Compact(), (ISpecialStore x) => Contract.Requires(x.Count > 0));
        Dbc.Def((ISpecialStore x, string item) => x.Add(item), (ISpecialStore x, string item) => Contract.Ensures(x.Count > 0));
        Dbc.Def((ISpecialStore x) => Contract.Invariant(x.Count <= x.Limit));
        """));
}
