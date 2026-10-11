using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.CreditDebit;

/// <summary>An account that can never go below zero.</summary>
[ContractClass(typeof(SimpleAccountContracts))]
public interface ISimpleAccount
{
    float Balance { get; }

    void Credit(float amount);

    void Debit(float amount);
}

public sealed class SimpleAccount(float openingBalance) : ISimpleAccount
{
    public float Balance { get; private set; } = openingBalance;

    public void Credit(float amount) => Balance += amount;

    public void Debit(float amount) => Balance -= amount;
}
