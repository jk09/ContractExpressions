#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using ContractExpressions4;

namespace ContractExpressions4.Tests.Demo.Operations;

[ContractClassFor(typeof(IOperationDemo))]
internal sealed class OperationDemoContracts
{
    public OperationDemoContracts()
    {
        Dbc.Def(static (IOperationDemo x) => x.ArgumentTypes,
            static (IOperationDemo x) => Contract.Ensures(Contract.Result<Type[]>() != null),
            static (IOperationDemo x) => Contract.Ensures(Contract.Result<Type[]>().Length == 2));

        Dbc.Def(static (IOperationDemo x) => x.ResultType,
            static (IOperationDemo x) => Contract.Ensures(Contract.Result<Type>() != null));

        Dbc.Def(static (IOperationDemo x, object[] arguments) => x.Perform(arguments),
            static (IOperationDemo x, object[] arguments) => Contract.Requires(arguments != null),
            static (IOperationDemo x, object[] arguments) => Contract.Requires(arguments.Length == x.ArgumentTypes.Length),
            static (IOperationDemo x, object[] arguments) => Contract.Requires(Contract.ForAll(0, arguments.Length, i => arguments[i] != null)),
            static (IOperationDemo x, object[] arguments) => Contract.Requires(Contract.ForAll(0, arguments.Length, i => x.ArgumentTypes[i].IsAssignableFrom(arguments[i].GetType()))),
            static (IOperationDemo x, object[] arguments) => Contract.Ensures(Contract.Result<object>() != null),
            static (IOperationDemo x, object[] arguments) => Contract.Ensures(x.ResultType.IsAssignableFrom(Contract.Result<object>().GetType())));
    }
}
