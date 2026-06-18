#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Demo.Devscovery;

[ContractClassFor(typeof(IArrayExtensionsDemo))]
internal sealed class ArrayExtensionsDemoContracts
{
    public ArrayExtensionsDemoContracts()
    {
        Dbc.Def(static (IArrayExtensionsDemo x, int[] xs) => x.Abs(xs),
            static (IArrayExtensionsDemo x, int[] xs) => Contract.Requires(xs != null),
            static (IArrayExtensionsDemo x, int[] xs) => Contract.Requires(Contract.ForAll(xs, n => n > int.MinValue)),
            static (IArrayExtensionsDemo x, int[] xs) => Contract.Ensures(Contract.Result<int[]>() != null),
            static (IArrayExtensionsDemo x, int[] xs) => Contract.Ensures(Contract.ForAll(Contract.Result<int[]>(), n => n >= 0)));
    }
}
