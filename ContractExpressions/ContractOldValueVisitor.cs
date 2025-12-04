using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

internal class ContractOldValueVisitor : ExpressionVisitor
{
    public List<PropertyInfo> OldValueProperties { get; } = new();

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == "OldValue")
        {
            var valueExpr = node.Arguments[0];

            if (valueExpr is MemberExpression oldValueMemberExpr && oldValueMemberExpr.Member is PropertyInfo propertyInfo)
            {
                OldValueProperties.Add(propertyInfo);

            }
            else
            {
                throw new NotImplementedException($"Cannot handle old value expression {valueExpr}");
            }
        }
        return base.VisitMethodCall(node);
    }

}


internal class ContractAssertVisitor : ExpressionVisitor
{
    public List<(Expression Condition, Expression? Message)> Assertions { get; } = new();

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == nameof(Contract.Assert))
        {
            var condition = node.Arguments[0];
            var message = node.Arguments.Count > 1 ? node.Arguments[1] : null;

            Assertions.Add((condition, message));
        }

        return base.VisitMethodCall(node);
    }
}

internal class ContractAssumeVisitor : ExpressionVisitor
{
    public List<(Expression Condition, Expression? Message)> Assumptions { get; } = new();

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name == nameof(Contract.Assume))
        {
            var condition = node.Arguments[0];
            var message = node.Arguments.Count > 1 ? node.Arguments[1] : null;

            Assumptions.Add((condition, message));
        }

        return base.VisitMethodCall(node);
    }
}

