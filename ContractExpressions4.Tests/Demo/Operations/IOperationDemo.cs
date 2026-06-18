#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractExpressions4.Tests.Demo.Operations;

[ContractClass(typeof(OperationDemoContracts))]
internal interface IOperationDemo
{
    Type[] ArgumentTypes { get; }
    Type ResultType { get; }

    object Perform(params object[] arguments);
}
