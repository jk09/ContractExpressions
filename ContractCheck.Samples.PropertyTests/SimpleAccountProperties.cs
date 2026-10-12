using ContractCheck.Samples.CreditDebit;

namespace ContractCheck.Samples.PropertyTests;

/// <summary>
/// Properties of <see cref="ISimpleAccount"/>. The single-call properties port the ContractExpressions4 demo properties
/// (Demo/CreditDebit/SimpleAccount.Tests.cs).
/// </summary>
[Properties(Arbitrary = [typeof(Generators)])]
public class SimpleAccountProperties
{
    private static ISimpleAccount NewAccount() => Dbc.Make<ISimpleAccount>(new SimpleAccount(100));

    [Property]
    public Property Credit_keeps_the_contracts(float amount) =>
        DbcPropertyTest.Check(NewAccount, account => account.Credit(amount));

    [Property]
    public Property Debit_keeps_the_contracts(float amount) =>
        DbcPropertyTest.Check(NewAccount, account => account.Debit(amount));

    [Property]
    public Property Credits_and_debits_keep_the_contracts(Money opening, SimpleAccountStep[] steps) =>
        DbcPropertyTest.CheckSequence(
            () => Dbc.Make<ISimpleAccount>(new SimpleAccount(opening.Value)),
            steps.Select(step => (Action<ISimpleAccount>)step.Apply));

    /// <summary>
    /// The same known defect, found by calls generated from the interface alone – FsCheck shrinks it to
    /// <c>[Credit(Infinity); Debit(Infinity)]</c>. 1000 cases, because a single 100-case run misses it about one time in seven.
    /// </summary>
    [Fact]
    public void Known_defect_random_calls_find_the_infinite_amounts_on_their_own() =>
        PropertyAssert.Falsified(DbcPropertyTest.CheckCalls(NewAccount), "Postcondition failed: x.Balance ==", maxTest: 1000);

    /// <summary>
    /// Known defect: the preconditions admit infinite amounts. <c>Credit(∞)</c> makes the balance ∞, and <c>Debit(∞)</c>
    /// (allowed: ∞ ≤ ∞) leaves <c>NaN</c>, so the <c>Debit</c> postcondition fails. FsCheck's own floats are ±∞ or NaN only
    /// about 6% of the time, so this property uses an edge-value generator. Fix: <c>Contract.Requires(float.IsFinite(amount))</c>
    /// on both members (or a decimal balance), then delete this test.
    /// </summary>
    [Fact]
    public void Known_defect_infinite_amounts_leave_a_NaN_balance()
    {
        Arbitrary<float> edgeAmounts = Arb.From(Gen.Elements(float.PositiveInfinity, float.MaxValue, float.Epsilon, 1f));

        PropertyAssert.Falsified(
            Prop.ForAll(edgeAmounts, amount => DbcPropertyTest.CheckSequence(NewAccount, [a => a.Credit(amount), a => a.Debit(amount)])),
            "Postcondition failed: x.Balance ==");
    }
}
