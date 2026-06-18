#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Demo.Devscovery;

[ContractClass(typeof(MyMathContracts))]
internal interface IMyMath
{
    int AbsNetStyle(int x);
    int AbsJavaStyle(int x);
}
