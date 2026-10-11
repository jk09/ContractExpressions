using ContractCheck.Samples.Max;

namespace ContractCheck.Samples.PropertyTests;

/// <summary>
/// Properties of <see cref="IMaxUtils"/>; <see cref="Max_keeps_the_contracts_for_non_empty_arrays"/> ports the
/// ContractExpressions4 demo property (Demo/Max/MaxDemoTests.cs).
/// </summary>
public class MaxUtilsProperties
{
    private static IMaxUtils NewUtils() => Dbc.Make<IMaxUtils>(new MaxUtils());

    [Property]
    public Property Max_keeps_the_contracts_for_non_empty_arrays(NonEmptyArray<int> elements) =>
        DbcPropertyTest.Check(NewUtils, utils => utils.Max(elements.Get));

    [Property]
    public Property ParseToInts_keeps_the_contracts(string[] original) =>
        DbcPropertyTest.Check(NewUtils, utils => utils.ParseToInts(original));

    /// <summary>
    /// Known defect, the point of the legacy demo (CodeContracts/Demo/Max): nothing requires a non-empty array, but
    /// <c>Max</c> of an empty array returns <see cref="int.MinValue"/>, which no element equals, so the <c>Exists</c>
    /// postcondition fails. One array in four is empty here. Fix: <c>Contract.Requires(elements.Length &gt; 0)</c>.
    /// </summary>
    [Fact]
    public void Known_defect_Max_of_an_empty_array_breaks_the_Exists_postcondition()
    {
        Arbitrary<int[]> arrays = Arb.From(Gen.Frequency(
            (1, Gen.Constant(Array.Empty<int>())),
            (3, ArbMap.Default.GeneratorFor<int[]>())));

        PropertyAssert.Falsified(
            Prop.ForAll(arrays, elements => DbcPropertyTest.Check(NewUtils, utils => utils.Max(elements))),
            "Postcondition failed: Contract.Exists(elements, el => el == Contract.Result<int>())");
    }
}
