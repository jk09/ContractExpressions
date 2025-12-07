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

internal class ContractAssertPatcher : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "Assert")
        {
            var conditionArg = node.Arguments[0];
            var messageArg = node.Arguments.Count > 1 ? node.Arguments[1] : Expression.Constant(null, typeof(string));

            var patchMethod = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Assert), new Type[] { typeof(bool), typeof(string) })!;

            var e = Expression.Call(null, patchMethod, conditionArg, messageArg);
            return e;
        }

        return base.VisitMethodCall(node);
    }
}

internal class ContractAssumePatcher : ExpressionVisitor
{

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "Assume")
        {
            var conditionArg = node.Arguments[0];
            var messageArg = node.Arguments.Count > 1 ? node.Arguments[1] : Expression.Constant(null, typeof(string));

            var patchMethod = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Assume), new Type[] { typeof(bool), typeof(string) })!;

            var e = Expression.Call(null, patchMethod, conditionArg, messageArg);
            return e;
        }

        return base.VisitMethodCall(node);
    }
}

