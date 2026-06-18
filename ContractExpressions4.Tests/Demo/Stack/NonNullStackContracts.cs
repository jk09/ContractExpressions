#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Demo.Stack;

[ContractClassFor(typeof(INonNullStack))]
internal sealed class NonNullStackContracts
{
    public NonNullStackContracts()
    {
        Dbc.Def(static (INonNullStack x, string value) => x.Push(value),
            static (INonNullStack x, string value) => Contract.Requires(value != null));

        Dbc.Def(static (INonNullStack x) => x.Pop(),
            static (INonNullStack x) => Contract.Requires(!x.IsEmpty),
            static (INonNullStack x) => Contract.Ensures(Contract.Result<string>() != null));

        Dbc.Def(static (INonNullStack x) => Contract.Invariant(x.Count >= 0));
        Dbc.Def(static (INonNullStack x) => Contract.Invariant(x.IsEmpty == (x.Count == 0)));
    }
}
