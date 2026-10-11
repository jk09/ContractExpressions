using ContractCheck.Samples.BankAccount;

namespace ContractCheck.Samples.PropertyTests;

/// <summary>
/// Properties of <see cref="IAccount"/>. The single-call properties port the ContractExpressions4 demo properties
/// (Demo/BankAccount/BankAccountDemoTests.cs); the sequence property checks the invariants across many calls.
/// </summary>
[Properties(Arbitrary = [typeof(Generators)])]
public class AccountProperties
{
    private static IAccount NewAccount() => Dbc.Make<IAccount>(new Account(100, supportsOverdraft: true, overdraftLimit: 20));

    [Property]
    public Property Deposit_keeps_the_contracts(float deposit) =>
        DbcPropertyTest.Check(NewAccount, account => account.Deposit(deposit));

    /// <summary>Withdrawing exactly the 120 available is the known defect below, so it is left out here.</summary>
    [Property]
    public Property Withdraw_keeps_the_contracts(float amount) =>
        Prop.When(amount != 120f, () => DbcPropertyTest.Check(NewAccount, account => account.Withdraw(amount)));

    /// <summary>Withdrawals of exactly everything available are the known defect below, so they are skipped here.</summary>
    [Property]
    public Property Deposits_and_withdrawals_keep_the_contracts(AccountSetup setup, AccountStep[] steps) =>
        DbcPropertyTest.CheckSequence(
            setup.Create,
            steps.Select(step => (Action<IAccount>)(account =>
            {
                if (step.Deposit || step.Amount.Value != account.Amount + account.OverdraftLimit)
                {
                    step.Apply(account);
                }
            })));

    /// <summary>
    /// Known defect, kept from the legacy demo (CodeContracts/Demo/BankAccount): the <c>Withdraw</c> precondition admits
    /// withdrawing everything available (<c>Amount + OverdraftLimit</c>), but the strict invariant
    /// <c>Amount &gt; -OverdraftLimit</c> then fails. Fix the contract (<c>&gt;=</c> in the invariant, or <c>&lt;</c> in the
    /// precondition), then delete this test and the exclusions above.
    /// </summary>
    [Fact]
    public void Known_defect_withdrawing_everything_available_breaks_the_invariant() =>
        PropertyAssert.Falsified(
            Prop.ForAll(Generators.AccountSetup(), setup =>
                DbcPropertyTest.Check(setup.Create, account => account.Withdraw(account.Amount + account.OverdraftLimit))),
            "Invariant failed: x.Amount > -x.OverdraftLimit");
}
