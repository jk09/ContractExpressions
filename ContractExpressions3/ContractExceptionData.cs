using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions3;

internal record ContractExceptionData(MethodInfo TargetMethod, object?[] Arguments, Expression ContractExpression);

