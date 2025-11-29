using System.Linq.Expressions;

namespace ContractExpressions;

internal class Invokable
{
    public required Expression Expression { get; init; }
    public required Delegate Delegate { get; init; }
}
