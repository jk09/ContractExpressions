using System.Linq.Expressions;
using System.Reflection;

internal class MethodSelectVisitor : ExpressionVisitor
{
    public MethodInfo Method { get; private set; } = null!;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Method = node.Method;

        return base.VisitMethodCall(node);
    }
}
