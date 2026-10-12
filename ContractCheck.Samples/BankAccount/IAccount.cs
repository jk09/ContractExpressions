#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.BankAccount;

/// <summary>A bank account with an optional overdraft (CodeContracts/Demo/BankAccount).</summary>
[ContractClass(typeof(AccountContracts))]
public interface IAccount
{
    bool SupportsOverdraft { get; }

    float OverdraftLimit { get; }

    float Amount { get; }

    void Deposit(float deposit);

    void Withdraw(float withdrawAmount);
}
