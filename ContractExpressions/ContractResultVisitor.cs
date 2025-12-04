using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace ContractExpressions;

internal class ContractResultPatchVisitor : ExpressionVisitor
{
    private readonly Expression _contractContextArg;
    public ContractResultPatchVisitor(Expression contractContextArg)
    {
        _contractContextArg = contractContextArg;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == nameof(Contract.Result))
        {
            var patchMethodGen = typeof(ContractPatch).GetMethod("Result", 1, new Type[] { typeof(ContractContext) });
            var patchMethod = patchMethodGen!.MakeGenericMethod(node.Method.ReturnType);

            var e = Expression.Call(null, patchMethod, _contractContextArg);
            return e;
        }

        return base.VisitMethodCall(node);
    }
}


internal class ContractValueAtReturnPatchVisitor : ExpressionVisitor
{
    private readonly Expression _contractContextArg;

    public ContractValueAtReturnPatchVisitor(Expression contractContextArg)
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