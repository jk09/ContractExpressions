#define CONTRACTS_FULL

using System.Diagnostics.Contracts;

namespace ContractCheck.Samples.Operations;

/// <summary>An operation over boxed arguments (CodeContracts/Demo/Operations/IOperation.cs).</summary>
[ContractClass(typeof(OperationContracts))]
public interface IOperation
{
    Type[] ArgumentTypes { get; }

    Type ResultType { get; }

    object Perform(params object[] arguments);
}

/// <summary>
/// A two-argument operation. The legacy demo put this contract on the abstract class <c>BinaryOperation</c>;
/// ContractCheck contracts live on interfaces, and <see cref="IBinaryOperation"/> inherits the contracts of <see cref="IOperation"/>.
/// </summary>
[ContractClass(typeof(BinaryOperationContracts))]
public interface IBinaryOperation : IOperation
{
}

/// <summary>A one-argument operation.</summary>
[ContractClass(typeof(UnaryOperationContracts))]
public interface IUnaryOperation : IOperation
{
}
