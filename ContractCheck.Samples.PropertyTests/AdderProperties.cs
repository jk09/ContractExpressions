using ContractCheck.Samples.Addition;

namespace ContractCheck.Samples.PropertyTests;

/// <summary>Properties of <see cref="IAdder"/>; the first ports the ContractExpressions4 demo property (Demo/Addition/AdderDemoTests.cs).</summary>
public class AdderProperties
{
    [Property]
    public Property Add_keeps_the_contracts(int a, int b) =>
        DbcPropertyTest.Check(() => Dbc.Make<IAdder>(new Adder()), adder => adder.Add(a, b));

    /// <summary>The postcondition states wrap-around (<c>unchecked</c>) addition, so overflow keeps the contract.</summary>
    [Property]
    public Property Add_keeps_the_contracts_when_it_overflows(IntWithMinMax a, IntWithMinMax b) =>
        DbcPropertyTest.Check(() => Dbc.Make<IAdder>(new Adder()), adder => adder.Add(a.Item, b.Item));
}
