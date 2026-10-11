using ContractCheck.Samples.Devscovery;

namespace ContractCheck.Samples.PropertyTests;

/// <summary>
/// Properties of <see cref="IMyMath"/> and <see cref="IArrayExtensions"/>; the <c>int</c> / <c>int[]</c> ones port the
/// ContractExpressions4 demo properties (Demo/Devscovery/DevscoveryDemoTests.cs). FsCheck's <c>int</c>s stay within ±size
/// (100 by default) and never reach <see cref="int.MinValue"/>, so the extremes get their own properties with <see cref="IntWithMinMax"/>.
/// </summary>
public class MyMathProperties
{
    private static IMyMath NewMath() => Dbc.Make<IMyMath>(new MyMath());

    [Property]
    public Property AbsNetStyle_keeps_the_contracts(int value) =>
        DbcPropertyTest.Check(NewMath, math => math.AbsNetStyle(value));

    /// <summary><see cref="int.MinValue"/> fails the typed precondition (<see cref="OverflowException"/>) and is discarded.</summary>
    [Property]
    public Property AbsNetStyle_keeps_the_contracts_at_the_extremes(IntWithMinMax value) =>
        DbcPropertyTest.Check(NewMath, math => math.AbsNetStyle(value.Item));

    [Property]
    public Property AbsJavaStyle_keeps_the_contracts_at_the_extremes(IntWithMinMax value) =>
        DbcPropertyTest.Check(NewMath, math => math.AbsJavaStyle(value.Item));

    [Property]
    public Property Array_Abs_keeps_the_contracts(int[] values) =>
        DbcPropertyTest.Check(() => Dbc.Make<IArrayExtensions>(new ArrayExtensions()), arrays => arrays.Abs(values));

    /// <summary>
    /// Edge values per element, but not <see cref="int.MinValue"/>: an array of <see cref="IntWithMinMax"/> almost always
    /// contains it, the precondition rejects the whole array, and the property would exhaust its arguments.
    /// </summary>
    [Property]
    public Property Array_Abs_keeps_the_contracts_at_the_extremes() =>
        Prop.ForAll(
            Arb.From(Gen.ArrayOf(Gen.Frequency((1, Gen.Elements(int.MinValue + 1, int.MaxValue, 0, -1)), (4, ArbMap.Default.GeneratorFor<int>())))),
            values => DbcPropertyTest.Check(() => Dbc.Make<IArrayExtensions>(new ArrayExtensions()), arrays => arrays.Abs(values)));
}
