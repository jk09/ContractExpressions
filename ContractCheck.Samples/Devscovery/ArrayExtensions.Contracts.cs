#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Devscovery;

[ContractClassFor(typeof(IArrayExtensions))]
internal sealed class ArrayExtensionsContracts
{
    public ArrayExtensionsContracts()
    {
        Dbc.Def(static (IArrayExtensions a, int[] xs) => a.Abs(xs),
            static (IArrayExtensions a, int[] xs) => Contract.Requires(xs != null),
            static (IArrayExtensions a, int[] xs) => Contract.Requires(Contract.ForAll(xs, x => x > int.MinValue)),
            static (IArrayExtensions a, int[] xs) => Contract.Ensures(Contract.Result<int[]>() != null),
            static (IArrayExtensions a, int[] xs) => Contract.Ensures(Contract.ForAll(Contract.Result<int[]>(), x => x >= 0)));
    }
}
