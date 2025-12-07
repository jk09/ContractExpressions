using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

internal class ContractOldValuePatcher : ExpressionVisitor
{
    private readonly Expression _contractContextArg;
    public ContractOldValuePatcher(Expression contractContextArg)
    {
        _contractContextArg = contractContextArg;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "OldValue")
        {
            var patchMethodGen = typeof(ContractPatch).GetMethod("OldValue", 1, new Type[] { typeof(PropertyInfo), typeof(ContractContext) });
            var patchMethod = patchMethodGen!.MakeGenericMethod(node.Method.ReturnType);

            var valueExpr = node.Arguments[0];

            if (valueExpr is MemberExpression oldValueMemberExpr && oldValueMemberExpr.Member is PropertyInfo propertyInfo)
            {
                var e = Expression.Call(null, patchMethod, Expression.Constant(propertyInfo, typeof(PropertyInfo)), _contractContextArg);
                return e;

            }
            else
            {
                throw new NotImplementedException($"Cannot handle old value expression {valueExpr}");
            }
        }

        return base.VisitMethodCall(node);
    }
}
