#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Operations;

[ContractClassFor(typeof(IOperation))]
internal sealed class OperationContracts
{
    public OperationContracts()
    {
        Dbc.Def(static (IOperation o) => o.ArgumentTypes,
            static (IOperation o) => Contract.Ensures(Contract.Result<Type[]>() != null));

        Dbc.Def(static (IOperation o) => o.ResultType,
            static (IOperation o) => Contract.Ensures(Contract.Result<Type>() != null));

        Dbc.Def(static (IOperation o, object[] arguments) => o.Perform(arguments),
            static (IOperation o, object[] arguments) => Contract.Requires(arguments != null),
            static (IOperation o, object[] arguments) => Contract.Requires(arguments.Length == o.ArgumentTypes.Length),
            static (IOperation o, object[] arguments) => Contract.Requires(Contract.ForAll(0, arguments.Length, i => arguments[i] != null)),
            static (IOperation o, object[] arguments) => Contract.Requires(Contract.ForAll(0, arguments.Length, i => o.ArgumentTypes[i].IsAssignableFrom(arguments[i].GetType()))),
            static (IOperation o, object[] arguments) => Contract.Ensures(Contract.Result<object>() != null),
            static (IOperation o, object[] arguments) => Contract.Ensures(o.ResultType.IsAssignableFrom(Contract.Result<object>().GetType())));
    }
}

[ContractClassFor(typeof(IBinaryOperation))]
internal sealed class BinaryOperationContracts
{
    public BinaryOperationContracts()
    {
        Dbc.Def(static (IBinaryOperation o) => o.ArgumentTypes,
            static (IBinaryOperation o) => Contract.Ensures(Contract.Result<Type[]>().Length == 2));
    }
}

[ContractClassFor(typeof(IUnaryOperation))]
internal sealed class UnaryOperationContracts
{
    public UnaryOperationContracts()
    {
        Dbc.Def(static (IUnaryOperation o) => o.ArgumentTypes,
            static (IUnaryOperation o) => Contract.Ensures(Contract.Result<Type[]>().Length == 1));
    }
}
