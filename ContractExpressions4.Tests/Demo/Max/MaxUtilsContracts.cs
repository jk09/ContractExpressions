#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Demo.Max;

[ContractClassFor(typeof(IMaxUtils))]
internal sealed class MaxUtilsContracts
{
    public MaxUtilsContracts()
    {
        Dbc.Def(static (IMaxUtils x, int[] elements) => x.Max(elements),
            static (IMaxUtils x, int[] elements) => Contract.Requires(elements != null),
            static (IMaxUtils x, int[] elements) => Contract.Ensures(Contract.ForAll(elements, el => el <= Contract.Result<int>())),
            static (IMaxUtils x, int[] elements) => Contract.Ensures(Contract.Exists(elements, el => el == Contract.Result<int>())));

        Dbc.Def(static (IMaxUtils x, string[] original) => x.ParseToInts(original),
            static (IMaxUtils x, string[] original) => Contract.Requires(original != null));
    }
}
