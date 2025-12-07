using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

internal class ContractValueAtReturnPatcher : ExpressionVisitor
{
    private readonly Expression _contractContextArg;

    public ContractValueAtReturnPatcher(Expression contractContextArg)
    {
        _contractContextArg = contractContextArg;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == nameof(Contract.ValueAtReturn))
        {
            var parameterArg = node.Arguments[0];
            var parameterInfo = (parameterArg as ParameterExpression);

            var patchMethodGen = typeof(ContractPatch).GetMethod(nameof(ContractPatch.ValueAtReturn));
            var patchMethod = patchMethodGen!.MakeGenericMethod(node.Method.ReturnType);

            var parameterInfoExpr = Expression.Constant(parameterInfo, typeof(System.Reflection.ParameterInfo));
            var e = Expression.Call(null, patchMethod, parameterInfoExpr, _contractContextArg);
            return e;
        }

        return base.VisitMethodCall(node);
    }
}