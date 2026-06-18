#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Demo.BankAccount;

[ContractClass(typeof(AccountContracts))]
internal interface IAccount
{
    bool SupportsOverdraft { get; }
    float OverdraftLimit { get; }
    float Amount { get; }

    void Deposit(float deposit);
    void Withdraw(float withdrawAmount);
}
