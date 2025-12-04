using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

internal class ContractValueAtReturnPatchVisitor : ExpressionVisitor
{
    private readonly Expression _contractContextArg;

    public ContractValueAtReturnPatchVisitor(Expression contractContextArg)
    {
        _contractContextArg = contractContextArg;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "ValueAtReturn")
        {
            var patchMethodGen = typeof(ContractPatch).GetMethod("ValueAtReturn", 1, new Type[] { typeof(ParameterInfo), typeof(ContractContext) });
            var patchMethod = patchMethodGen!.MakeGenericMethod(node.Method.ReturnType);

            var valueExpr = node.Arguments[0];

            if (valueExpr is ParameterExpression parameterExpr)
            {
                // Need to get the ParameterInfo from the parameter expression
                // This is a simplification - in real usage, we'd need more context
                throw new NotSupportedException("ValueAtReturn requires additional context about parameter metadata");
            }
            else if (valueExpr is ConstantExpression constantExpr && constantExpr.Value is ParameterInfo paramInfo)
            {
                var e = Expression.Call(null, patchMethod, Expression.Constant(paramInfo, typeof(ParameterInfo)), _contractContextArg);
                return e;
            }
            else
            {
                throw new NotImplementedException($"Cannot handle ValueAtReturn expression {valueExpr}");
            }
        }

        return base.VisitMethodCall(node);
    }
}
