#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Devscovery;

[ContractClassFor(typeof(IMyMath))]
internal sealed class MyMathContracts
{
    public MyMathContracts()
    {
        Dbc.Def(static (IMyMath m, int x) => m.AbsNetStyle(x),
            static (IMyMath m, int x) => Contract.Requires<OverflowException>(x > int.MinValue),
            static (IMyMath m, int x) => Contract.Ensures(Contract.Result<int>() >= 0));

        Dbc.Def(static (IMyMath m, int x) => m.AbsJavaStyle(x),
            static (IMyMath m, int x) => Contract.Ensures(x == int.MinValue ? Contract.Result<int>() == x : Contract.Result<int>() >= 0));
    }
}
