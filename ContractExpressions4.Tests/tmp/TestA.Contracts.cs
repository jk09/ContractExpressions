#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Tmp;

[ContractClassFor(typeof(ITestA))]
public class TestAContracts
{
    public TestAContracts()
    {
        Dbc.Def(static (ITestA x, int xArg, int yArg) => x.M(xArg, yArg),
            static (ITestA x, int xArg, int yArg) => Contract.Requires(yArg != 0));
    }
}
