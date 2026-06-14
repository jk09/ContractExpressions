using System.Linq.Expressions;

namespace ContractExpressions2;

internal record Invokable(
    Expression Expression,
    Delegate Delegate);
