namespace ContractExpressions4.Tests.Demo.BankAccount;

internal sealed class Account(float openingAmount, bool supportsOverdraft = false, float overdraftLimit = 0) : IAccount
{
    private float amount = openingAmount;
    private readonly bool supportsOverdraftFlag = supportsOverdraft;
    private readonly float overdraftLimitValue = overdraftLimit;

    public bool SupportsOverdraft => supportsOverdraftFlag;
    public float OverdraftLimit => overdraftLimitValue;
    public float Amount => amount;

    public void Deposit(float deposit)
    {
        amount += deposit;
    }

    public void Withdraw(float withdrawAmount)
    {
        amount -= withdrawAmount;
    }
}
