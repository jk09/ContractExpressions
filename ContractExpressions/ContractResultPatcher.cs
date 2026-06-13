using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace ContractExpressions;

internal class ContractResultPatcher : ExpressionVisitor
{
    private readonly Expression _contractContextArg;
    public ContractResultPatcher(Expression contractContextArg)
    {
        _contractContextArg = contractContextArg;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == nameof(Contract.Result))
        {
            var patchMethodGen = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Result), 1, new Type[] { typeof(ContractContext) });
            var patchMethod = patchMethodGen!.MakeGenericMethod(node.Method.ReturnType);

            var e = Expression.Call(null, patchMethod, _contractContextArg);
            return e;
        }

        return base.VisitMethodCall(node);
    }
}

