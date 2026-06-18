#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Demo.Devscovery;

[ContractClassFor(typeof(IMyMath))]
internal sealed class MyMathContracts
{
    public MyMathContracts()
    {
        Dbc.Def(static (IMyMath x, int value) => x.AbsNetStyle(value),
            static (IMyMath x, int value) => Contract.Requires(value > int.MinValue),
            static (IMyMath x, int value) => Contract.Ensures(Contract.Result<int>() >= 0));

        Dbc.Def(static (IMyMath x, int value) => x.AbsJavaStyle(value),
            static (IMyMath x, int value) => Contract.Ensures(value == int.MinValue
                ? Contract.Result<int>() == value
                : Contract.Result<int>() >= 0));
    }
}
