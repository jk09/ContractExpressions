namespace ContractCheck.Samples.BankAccount;

/// <summary>
/// The legacy demo also had constructor contracts (<c>openingAmount &gt; -overdraftLimit</c>, ...). Constructors are not
/// interface members, so ContractCheck checks the same facts through the interface invariants when <c>Dbc.Make</c> wraps the account.
/// </summary>
public sealed class Account(float openingAmount, bool supportsOverdraft = false, float overdraftLimit = 0) : IAccount
{
    private float amount = openingAmount;

    public bool SupportsOverdraft { get; } = supportsOverdraft;

    public float OverdraftLimit { get; } = overdraftLimit;

    public float Amount => amount;

    public void Deposit(float deposit) => amount += deposit;

    public void Withdraw(float withdrawAmount) => amount -= withdrawAmount;
}
