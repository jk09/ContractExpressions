using System.Linq.Expressions;

namespace ContractExpressions3;

internal record Invokable(
    Expression Expression,
    Delegate Delegate);

