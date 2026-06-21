#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Tmp;

[ContractClassFor(typeof(IMultiplier))]
public class MultiplierContracts
{
    public MultiplierContracts()
    {
        Dbc.Def(static (IMultiplier x, int a, int b) => x.Multiply(a, b),
            static (IMultiplier x, int a, int b) => Contract.Requires(a > 0 && b > 0),
            static (IMultiplier x, int a, int b) => Contract.Ensures(Contract.Result<int>() > 0));
    }
}
