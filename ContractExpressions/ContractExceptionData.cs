using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

internal record ContractExceptionData(MethodInfo TargetMethod, object?[] Arguments, Expression ContractExpression);
