#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Demo.CreditDebit;

[ContractClass(typeof(SimpleAccountContracts))]
internal interface ISimpleAccount
{
    float Balance { get; }

    void Credit(float amount);
    void Debit(float amount);
}

internal sealed class SimpleAccount(float openingBalance) : ISimpleAccount
{
    private float balance = openingBalance;

    public float Balance => balance;

    public void Credit(float amount)
    {
        balance += amount;
    }

    public void Debit(float amount)
    {
        balance -= amount;
    }
}
