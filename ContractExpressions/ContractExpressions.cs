#define CONTRACTS_FULL

using System.Collections;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace ContractExpressions;

static class Dbc
{
    public static void Def<TContract, TPar1, TRet>(Expression<Func<TContract, TPar1, TRet>> method, params Expression<Action<TContract, TPar1>>[] contracts)
    {
        foreach (var contract in contracts)
        {
            var visitor = new DbcDefVisitor();
            visitor.Visit(contract);

        }
    }
}

class ContractResultVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "Result")
        {
            node.Dump("result");
        }
        return base.VisitMethodCall(node);
    }
}

class ContractOldValueVisitor : ExpressionVisitor
{
    public List<MemberInfo> OldValueMembers { get; } = new();

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "OldValue")
        {
            node.Dump("old value");

            var valueExpr = node.Arguments[0];

            if (valueExpr is MemberExpression oldValueMemberExpr)
            {
                var oldValueMember = oldValueMemberExpr.Member;
                OldValueMembers.Add(oldValueMember);

            }
            else
            {
                throw new NotImplementedException($"Cannot handle old value expression {valueExpr}");
            }
        }
        return base.VisitMethodCall(node);
    }

}

class DbcDefVisitor : ExpressionVisitor
{
    private IList<ParameterExpression>? _contractParameters;

    public override Expression? Visit(Expression? node)
    {
        if (_contractParameters == null && node is LambdaExpression lambda)
        {
            _contractParameters = lambda.Parameters;
            //node.Dump("node");
        }

        return base.Visit(node);

    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract))
        {
            if (node.Method.Name == "Requires")
            {
                var contractBody = node.Arguments[0];

                var contract = Expression.Lambda(contractBody, $"Requires_1", _contractParameters);

                var dlg = contract.Compile();

                dlg.DynamicInvoke(null, "").Dump("outcome");
            }
            else if (node.Method.Name == "Ensures")
            {
                var contractBody = node.Arguments[0];
                var contract = Expression.Lambda(contractBody, $"Ensures_1", _contractParameters);
                contract.Dump("ensures");

                var oldValueVisitor = new ContractOldValueVisitor();
                oldValueVisitor.Visit(contractBody);


            }
        }
        return base.VisitMethodCall(node);
    }


}
