using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace ContractExpr;

internal class ContractResultPatchVisitor : ExpressionVisitor
{
    private readonly Expression _contractContextArg;
    public ContractResultPatchVisitor(Expression contractContextArg)
    {
        _contractContextArg = contractContextArg;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "Result")
        {
            var patchMethodGen = typeof(ContractPatch).GetMethod("Result", 1, new Type[] { typeof(ContractContext) });
            var patchMethod = patchMethodGen!.MakeGenericMethod(node.Method.ReturnType);

            var e = Expression.Call(null, patchMethod, _contractContextArg);
            return e;
        }

        return base.VisitMethodCall(node);
    }
}
