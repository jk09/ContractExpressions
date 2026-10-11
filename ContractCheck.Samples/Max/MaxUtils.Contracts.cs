#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Max;

[ContractClassFor(typeof(IMaxUtils))]
internal sealed class MaxUtilsContracts
{
    public MaxUtilsContracts()
    {
        Dbc.Def(static (IMaxUtils u, int[] elements) => u.Max(elements),
            static (IMaxUtils u, int[] elements) => Contract.Requires(elements != null),
            static (IMaxUtils u, int[] elements) => Contract.Ensures(Contract.ForAll(elements, el => el <= Contract.Result<int>())),
            static (IMaxUtils u, int[] elements) => Contract.Ensures(Contract.Exists(elements, el => el == Contract.Result<int>())));

        Dbc.Def(static (IMaxUtils u, string[] original) => u.ParseToInts(original),
            static (IMaxUtils u, string[] original) => Contract.Requires(original != null));
    }
}
