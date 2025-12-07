using System.Linq.Expressions;

namespace ContractExpressions;

internal record Invokable(
    Expression Expression,
    Delegate Delegate);
