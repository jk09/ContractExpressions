using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

internal class DbcDefVisitor : ExpressionVisitor
{
    private IList<ParameterExpression>? _contractParameters;
    private readonly Type _contractType;

    public readonly List<Delegate> Preconditions = new();
    public readonly List<Delegate> Postconditions = new();
    public readonly Dictionary<PropertyInfo, Delegate> OldValueCollectors = new();

    public DbcDefVisitor(Type contractType)
    {
        _contractType = contractType;
    }

    public override Expression? Visit(Expression? node)
    {
        if (_contractParameters == null && node is LambdaExpression lambda)
        {
            _contractParameters = lambda.Parameters;
            //node.Dump("node");
        }

        return base.Visit(node);

    }

    private void AddOldValueCollectors(Expression condition)
    {
        var oldValueVisitor = new ContractOldValueVisitor();
        oldValueVisitor.Visit(condition);

        foreach (var oldValueMember in oldValueVisitor.OldValueProperties)
        {
            if (!OldValueCollectors.ContainsKey(oldValueMember))
            {
                var thisParamExpr = Expression.Parameter(_contractType, "thisContract");
                var collector = Expression.Lambda(Expression.Property(thisParamExpr, oldValueMember), thisParamExpr);
                var dlg = collector.Compile();

                OldValueCollectors.Add(oldValueMember, dlg);
            }
        }
    }

    private static Expression WithPatchedResults(Expression condition, ParameterExpression contractContextParam)
    {
        var resultPatcher = new ContractResultPatchVisitor(contractContextParam);
        var patched = resultPatcher.Visit(condition);
        return patched;
    }

    private static Expression WithPatchedOldValues(Expression condition, ParameterExpression contractContextParam)
    {
        var oldValuePatcher = new ContractOldValuePatchVisitor(contractContextParam);
        var patched = oldValuePatcher.Visit(condition);
        return patched;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Contract))
        {
            if (node.Method.Name == nameof(Contract.Requires))
            {
                var condition = node.Arguments[0];
                var message = node.Arguments.Count > 1 ? node.Arguments[1] : Expression.Constant(null, typeof(string));
                var exceptionType = node.Method.GetGenericArguments().Length > 0
                    ? Expression.Constant(node.Method.GetGenericArguments()[0], typeof(Type))
                    : Expression.Constant(null, typeof(Type));

                var preconditionPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Requires), new Type[] { typeof(bool), typeof(string), typeof(Type) })!;


                var preconditionParams = new List<ParameterExpression>(_contractParameters!);

                var contract = Expression.Lambda(Expression.Call(null, preconditionPatch, condition, message, exceptionType), $"Requires_1", preconditionParams);

                var contractDlg = contract.Compile();

                Preconditions.Add(contractDlg);
            }
            else if (node.Method.Name == nameof(Contract.Ensures))
            {
                var condition = node.Arguments[0];
                var message = node.Arguments.Count > 1 ? node.Arguments[1] : Expression.Constant(null, typeof(string));
                var exceptionType = node.Method.GetGenericArguments().Length > 0
                    ? Expression.Constant(node.Method.GetGenericArguments()[0], typeof(Type))
                    : Expression.Constant(null, typeof(Type));

                AddOldValueCollectors(condition);

                var contractContextParam = Expression.Parameter(typeof(ContractContext), "contractContext");

                var patchedCondition = WithPatchedOldValues(WithPatchedResults(condition, contractContextParam), contractContextParam);

                var postconditionPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Ensures), new Type[] { typeof(bool), typeof(string), typeof(Type) })!;

                var postconditionParams = new List<ParameterExpression>(_contractParameters!)
                {
                    contractContextParam
                };

                var contract = Expression.Lambda(Expression.Call(null, postconditionPatch, patchedCondition, message, exceptionType), $"Ensures_1", postconditionParams);

                var contractDlg = contract.Compile();

                Postconditions.Add(contractDlg);

            }
        }

        return base.VisitMethodCall(node);
    }


}
