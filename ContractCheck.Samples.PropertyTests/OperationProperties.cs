using ContractCheck.Samples.Operations;

namespace ContractCheck.Samples.PropertyTests;

/// <summary>
/// Properties of <see cref="IBinaryOperation"/> and <see cref="IUnaryOperation"/> (and the <see cref="IOperation"/>
/// contracts they inherit). The <see cref="IntAdder"/> properties port the ContractExpressions4 demo properties
/// (Demo/Operations/OperationsDemoTests.cs).
/// </summary>
public class OperationProperties
{
    [Property]
    public Property Perform_keeps_the_contracts(int left, int right) =>
        DbcPropertyTest.Check(() => Dbc.Make<IBinaryOperation>(new IntAdder()), operation => operation.Perform(left, right));

    [Property]
    public Property ArgumentTypes_keeps_the_contracts() =>
        DbcPropertyTest.Check(() => Dbc.Make<IBinaryOperation>(new IntAdder()), operation => operation.ArgumentTypes);

    [Property]
    public Property ResultType_keeps_the_contracts() =>
        DbcPropertyTest.Check(() => Dbc.Make<IBinaryOperation>(new IntAdder()), operation => operation.ResultType);

    [Property]
    public Property Subtraction_and_multiplication_keep_the_contracts(IntWithMinMax left, IntWithMinMax right) =>
        DbcPropertyTest.Check(() => Dbc.Make<IBinaryOperation>(new IntSub()), operation => operation.Perform(left.Item, right.Item))
            .And(DbcPropertyTest.Check(() => Dbc.Make<IBinaryOperation>(new IntMul()), operation => operation.Perform(left.Item, right.Item)));

    [Property]
    public Property Unary_minus_keeps_the_contracts(IntWithMinMax value) =>
        DbcPropertyTest.Check(() => Dbc.Make<IUnaryOperation>(new IntUnaryMinus()), operation => operation.Perform(value.Item));

    /// <summary>
    /// Wrong arities are rejected by the inherited preconditions and discarded. The argument count comes from 0–4: with
    /// FsCheck's arrays (up to 100 long) almost every case would be discarded and the property would exhaust its arguments.
    /// </summary>
    [Property]
    public Property Perform_with_any_number_of_arguments_keeps_the_contracts() =>
        Prop.ForAll(
            Arb.From(Gen.Choose(0, 4).SelectMany(count => Gen.ArrayOf(ArbMap.Default.GeneratorFor<int>(), count))),
            values => DbcPropertyTest.Check(() => Dbc.Make<IBinaryOperation>(new IntAdder()), operation => operation.Perform([.. values.Cast<object>()])));

    /// <summary>
    /// Known defect, kept from the legacy demo: division adds no precondition, so <c>Perform(x, 0)</c> passes every contract
    /// check and then throws <see cref="DivideByZeroException"/> – an exception the contract does not allow for. Fix: an
    /// <c>IDivision : IBinaryOperation</c> interface whose contract requires a non-zero divisor.
    /// </summary>
    [Fact]
    public void Known_defect_division_by_zero_escapes_the_contract() =>
        PropertyAssert.Falsified(
            Prop.ForAll(Arb.From(Gen.Choose(-2, 2)), divisor =>
                DbcPropertyTest.Check(() => Dbc.Make<IBinaryOperation>(new IntDiv()), operation => operation.Perform(10, divisor))),
            nameof(DivideByZeroException));
}
