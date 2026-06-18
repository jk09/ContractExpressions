#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Demo.BankAccount;

[ContractClassFor(typeof(IAccount))]
internal sealed class AccountContracts
{
    public AccountContracts()
    {
        Dbc.Def(static (IAccount x, float deposit) => x.Deposit(deposit),
            static (IAccount x, float deposit) => Contract.Requires(deposit > 0.0f));

        Dbc.Def(static (IAccount x, float withdrawAmount) => x.Withdraw(withdrawAmount),
            static (IAccount x, float withdrawAmount) => Contract.Requires(withdrawAmount <= x.Amount + x.OverdraftLimit),
            static (IAccount x, float withdrawAmount) => Contract.Ensures(x.Amount == Contract.OldValue<float>(x.Amount) - withdrawAmount));

        Dbc.Def(static (IAccount x) => Contract.Invariant(x.SupportsOverdraft ? x.OverdraftLimit > 0 : x.OverdraftLimit == 0));
        Dbc.Def(static (IAccount x) => Contract.Invariant(x.OverdraftLimit <= 1000));
        Dbc.Def(static (IAccount x) => Contract.Invariant(x.Amount > -x.OverdraftLimit));
    }
}
