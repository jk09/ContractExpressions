#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Tests.Definitions.Validation;

/// <summary>
/// Malformed clauses are rejected when they are defined, mirroring the CodeContracts checker errors
/// (Foxtrot/Tests/CheckerTests: CC1000, CC1001, CC1002, CC1023). ContractExpressions and ContractExpressions4
/// accepted most of these and then evaluated <c>Result</c>/<c>OldValue</c> as <c>default</c> (resolutions R6, R7).
/// </summary>
public class ClauseValidationTests
{
    [Fact]
    public void Result_in_a_precondition_is_rejected_CC1023()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Requires(Contract.Result<int>() == 0)),
            "Contract.Result can only be used in Ensures clauses");
    }

    [Theory]
    [InlineData("Assert")]
    [InlineData("Assume")]
    [InlineData("Invariant")]
    public void Result_outside_Ensures_is_rejected(string kind)
    {
        Action define = kind switch
        {
            "Assert" => () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Assert(Contract.Result<int>() > 0)),
            "Assume" => () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Assume(Contract.Result<int>() > 0)),
            _ => () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Invariant(Contract.Result<int>() > 0)),
        };

        Expect.DefinitionError(define, "Contract.Result can only be used in Ensures clauses");
    }

    [Fact]
    public void Result_in_EnsuresOnThrow_is_rejected_CC1001()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.EnsuresOnThrow<Exception>(Contract.Result<int>() == 0)),
            "Contract.Result cannot be used in EnsuresOnThrow", "CC1001");
    }

    [Fact]
    public void Result_with_the_wrong_type_is_rejected_CC1002()
    {
        // ContractExpressions silently used default(long) and ContractExpressions4 threw InvalidCastException at call time (probe S07).
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Ensures(Contract.Result<long>() == 5L)),
            "Contract.Result<long>() does not match the return type", "Contract.Result<int>()", "CC1002");
    }

    [Fact]
    public void Result_with_a_base_type_of_the_return_type_is_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x) => x.Name(), static (IValidated x) => Contract.Ensures(Contract.Result<object>() != null)),
            "does not match the return type");
    }

    [Fact]
    public void Result_of_a_void_member_is_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x) => x.Reset(), static (IValidated x) => Contract.Ensures(Contract.Result<int>() == 0)),
            "returns void");
    }

    [Fact]
    public void OldValue_in_a_precondition_is_rejected_CC1023()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Requires(Contract.OldValue(x.Total) >= 0)),
            "Contract.OldValue can only be used in Ensures and EnsuresOnThrow clauses");
    }

    [Fact]
    public void OldValue_in_an_invariant_is_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x) => Contract.Invariant(x.Total >= Contract.OldValue(x.Total))),
            "Contract.OldValue can only be used in Ensures and EnsuresOnThrow clauses");
    }

    [Fact]
    public void OldValue_of_a_literal_is_rejected_CC1000()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Ensures(Contract.OldValue(5) == 5)),
            "Contract.OldValue of a literal", "CC1000");
    }

    [Fact]
    public void OldValue_containing_Result_is_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Ensures(Contract.OldValue(Contract.Result<int>()) == 0)),
            "cannot contain Contract.Result or another Contract.OldValue");
    }

    [Fact]
    public void OldValue_of_a_lambda_variable_outside_an_index_quantifier_is_rejected_with_a_hint()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int[] xs) => x.Fill(xs), static (IValidated x, int[] xs) => Contract.Ensures(Contract.ForAll(xs, e => e == Contract.OldValue(e)))),
            "Contract.OldValue(xs.ToArray())");
    }

    [Fact]
    public void ValueAtReturn_is_rejected_because_out_parameters_are_not_supported()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Ensures(Contract.ValueAtReturn(out OutPlaceholder.Value) > 0)),
            "Contract.ValueAtReturn", "ref/out parameters are not supported");
    }

    [Fact]
    public void EndContractBlock_is_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x) => x.Reset(), static (IValidated x) => Contract.EndContractBlock()),
            "Contract.EndContractBlock");
    }

    [Fact]
    public void A_clause_that_is_not_a_Contract_call_is_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x) => x.Reset(), static (IValidated x) => x.Reset()),
            "a contract clause must be a single call to System.Diagnostics.Contracts.Contract");
    }

    [Fact]
    public void A_quantifier_used_as_a_clause_is_rejected_with_a_hint()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int[] xs) => x.Fill(xs), static (IValidated x, int[] xs) => Contract.ForAll(xs, e => e > 0)),
            "Contract.ForAll is not a clause", "Contract.Ensures(Contract.ForAll(...))");
    }

    [Fact]
    public void Interface_level_definition_accepts_only_invariants()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x) => Contract.Requires(x.Total >= 0)),
            "can only define invariants");
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x) => Contract.Invariant(x.Total >= 0), static (IValidated x) => Contract.Ensures(x.Total >= 0)),
            "an interface-level Dbc.Def accepts only Contract.Invariant clauses");
    }

    [Fact]
    public void Requires_of_an_exception_type_that_cannot_be_constructed_is_rejected()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Requires<AbstractFailure>(v > 0)),
            "Contract.Requires<AbstractFailure> cannot be thrown", "is abstract");
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x, int v) => x.Compute(v), static (IValidated x, int v) => Contract.Requires<NoUsableConstructorFailure>(v > 0)),
            "has no public (string paramName, string message), (string message) or parameterless constructor");
    }

    [Fact]
    public void A_definition_with_an_invalid_clause_registers_none_of_its_clauses()
    {
        Assert.Throws<InvalidOperationException>(() => Dbc.Def(
            static (IValidated x, int v) => x.Compute(v),
            static (IValidated x, int v) => Contract.Requires(v != 13),
            static (IValidated x, int v) => Contract.Requires(Contract.Result<int>() == 0)));

        Assert.Equal(13, Dbc.Make<IValidated>(new Validated()).Compute(13));
    }

    [Fact]
    public void Clauses_for_async_members_cannot_read_both_the_task_and_its_result()
    {
        Expect.DefinitionError(
            () => Dbc.Def(static (IValidated x) => x.LoadAsync(), static (IValidated x) => Contract.Ensures(Contract.Result<Task<int>>() != null && Contract.Result<int>() > 0)),
            "cannot read both the returned task and its awaited result");
    }

    private static class OutPlaceholder
    {
        public static int Value;
    }
}

public interface IValidated
{
    int Total { get; }

    int Compute(int value);

    string Name();

    void Reset();

    void Fill(int[] values);

    Task<int> LoadAsync();
}

public sealed class Validated : IValidated
{
    public int Total { get; private set; }

    public int Compute(int value) => Total = value;

    public string Name() => "validated";

    public void Reset() => Total = 0;

    public void Fill(int[] values) => Array.Fill(values, 1);

    public Task<int> LoadAsync() => Task.FromResult(1);
}

public abstract class AbstractFailure(string message) : Exception(message);

public sealed class NoUsableConstructorFailure(int code) : Exception(code.ToString(System.Globalization.CultureInfo.InvariantCulture));
