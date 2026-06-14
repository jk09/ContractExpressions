using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

internal class MethodSelectVisitor : ExpressionVisitor
{
    public MethodInfo Method { get; private set; } = null!;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Capture only the outermost method call; nested calls inside arguments are ignored.
        if (Method == null!)
        {
            Method = node.Method;
        }
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // Handle property access (e.g. x.Count) when no method call was found first.
        if (Method == null! && node.Member is PropertyInfo property && property.GetMethod != null)
        {
            Method = property.GetMethod;
        }
        return base.VisitMember(node);
    }
}
