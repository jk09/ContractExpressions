#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Demo.CreditDebit;

[ContractClassFor(typeof(ISimpleAccount))]
internal sealed class SimpleAccountContracts
{
    public SimpleAccountContracts()
    {
        Dbc.Def(static (ISimpleAccount x) => x.Balance,
            static (ISimpleAccount x) => Contract.Ensures(Contract.Result<float>() >= 0));

        Dbc.Def(static (ISimpleAccount x, float amount) => x.Credit(amount),
            static (ISimpleAccount x, float amount) => Contract.Requires(amount > 0),
            static (ISimpleAccount x, float amount) => Contract.Ensures(x.Balance == Contract.OldValue<float>(x.Balance) + amount));

        Dbc.Def(static (ISimpleAccount x, float amount) => x.Debit(amount),
            static (ISimpleAccount x, float amount) => Contract.Requires(amount > 0),
            static (ISimpleAccount x, float amount) => Contract.Requires(amount <= x.Balance),
            static (ISimpleAccount x, float amount) => Contract.Ensures(x.Balance == Contract.OldValue<float>(x.Balance) - amount));

        Dbc.Def(static (ISimpleAccount x) => Contract.Invariant(x.Balance >= 0));
    }
}
