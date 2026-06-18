#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Demo.NonNullStackWithUpdate;

[ContractClassFor(typeof(INonNullStackWithUpdates))]
internal sealed class NonNullStackWithUpdatesContracts
{
    public NonNullStackWithUpdatesContracts()
    {
        Dbc.Def(static (INonNullStackWithUpdates x, string value) => x.Push(value),
            static (INonNullStackWithUpdates x, string value) => Contract.Requires(value != null));

        Dbc.Def(static (INonNullStackWithUpdates x) => x.Pop(),
            static (INonNullStackWithUpdates x) => Contract.Requires(!x.IsEmpty),
            static (INonNullStackWithUpdates x) => Contract.Ensures(Contract.Result<string>() != null));

        Dbc.Def(static (INonNullStackWithUpdates x, int index, string value) => x.UpdateAt(index, value),
            static (INonNullStackWithUpdates x, int index, string value) => Contract.Requires(index >= 0),
            static (INonNullStackWithUpdates x, int index, string value) => Contract.Requires(index < x.Count),
            static (INonNullStackWithUpdates x, int index, string value) => Contract.Requires(value != null));

        Dbc.Def(static (INonNullStackWithUpdates x) => Contract.Invariant(x.Count >= 0));
        Dbc.Def(static (INonNullStackWithUpdates x) => Contract.Invariant(x.IsEmpty == (x.Count == 0)));
    }
}
