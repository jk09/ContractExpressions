#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Addition;

[ContractClassFor(typeof(IAdder))]
internal sealed class AdderContracts
{
    public AdderContracts()
    {
        Dbc.Def(static (IAdder x, int a, int b) => x.Add(a, b),
            static (IAdder x, int a, int b) => Contract.Ensures(Contract.Result<int>() == unchecked(a + b)));
    }
}
