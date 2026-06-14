using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions2;

internal class MethodSelectVisitor : ExpressionVisitor
{
    public MethodInfo Method { get; private set; } = null!;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Method = node.Method;

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member is PropertyInfo property && property.GetMethod != null)
        {
            Method = property.GetMethod;
        }

        return base.VisitMember(node);
    }
}
