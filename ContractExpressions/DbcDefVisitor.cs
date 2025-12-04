using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

internal class DbcDefVisitor(Type contractType) : ExpressionVisitor
{
    private IList<ParameterExpression>? _contractParameters;
    private readonly Type _contractType = contractType;
    public readonly IList<Delegate> Preconditions = new List<Delegate>();
    public readonly IList<Delegate> Postconditions = new List<Delegate>();
    public readonly IList<Delegate> PostconditionsOnThrow = new List<Delegate>();

    public readonly IDictionary<PropertyInfo, Delegate> OldValueCollectors = new Dictionary<PropertyInfo, Delegate>();

    public override Expression? Visit(Expression? node)
    {
        if (_contractParameters == null && node is LambdaExpression lambda)
        {
            _contractParameters = lambda.Parameters;
        }

        return base.Visit(node);
    }

    private void AddOldValueCollectors(Expression condition, out IDictionary<PropertyInfo, Delegate> oldValueCollectors)
    {
        var oldValueVisitor = new ContractOldValueVisitor();
        oldValueVisitor.Visit(condition);
        oldValueCollectors = new Dictionary<PropertyInfo, Delegate>();

        foreach (var oldValueMember in oldValueVisitor.OldValueProperties)
        {
            if (!oldValueCollectors.ContainsKey(oldValueMember))
            {
                var thisParamExpr = Expression.Parameter(_contractType, "thisContract");
                var collector = Expression.Lambda(Expression.Property(thisParamExpr, oldValueMember), thisParamExpr);
                var dlg = collector.Compile();

                oldValueCollectors.Add(oldValueMember, dlg);
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

    private static Expression WithPatchedValueAtReturn(Expression condition, ParameterExpression contractContextParam)
    {
        var valueAtReturnPatcher = new ContractValueAtReturnPatchVisitor(contractContextParam);
        var patched = valueAtReturnPatcher.Visit(condition);
        return patched;
    }

    private void GetPreconditionDelegate(MethodCallExpression contractRequiresExpr, out Delegate contractDlg)
    {
        var condition = contractRequiresExpr.Arguments[0];
        var message = contractRequiresExpr.Arguments.Count > 1 ? contractRequiresExpr.Arguments[1] : Expression.Constant(null, typeof(string));

        Expression exceptionType;

        MethodInfo preconditionPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Requires), new Type[] { typeof(bool), typeof(string), typeof(Type) })!;

        if (contractRequiresExpr.Method.GetGenericArguments().Length > 0)
        {
            // Requires<TException>(bool, string)
            var exceptionTypeArg = contractRequiresExpr.Method.GetGenericArguments()[0];
            exceptionType = Expression.Constant(null, typeof(string)); // message already extracted
        }
        else
        {
            exceptionType = Expression.Constant(null, typeof(Type));
        }

        var preconditionParams = new List<ParameterExpression>(_contractParameters!);

        Expression callExpr = contractRequiresExpr.Method.GetGenericArguments().Length > 0
            ? Expression.Call(null, preconditionPatch, condition, message)
            : Expression.Call(null, preconditionPatch, condition, message, exceptionType);

        var contract = Expression.Lambda(callExpr, $"Requires_1", preconditionParams);
        contractDlg = contract.Compile();
    }

    private void GetPostconditionDelegate(MethodCallExpression contractEnsuresExpr, out Delegate contractDlg, out IDictionary<PropertyInfo, Delegate> oldValueCollectorDlgs)
    {
        var condition = contractEnsuresExpr.Arguments[0];
        var message = contractEnsuresExpr.Arguments.Count > 1 ? contractEnsuresExpr.Arguments[1] : Expression.Constant(null, typeof(string));
        var exceptionType = Expression.Constant(null, typeof(Type));

        AddOldValueCollectors(condition, out oldValueCollectorDlgs);

        var contractContextParam = Expression.Parameter(typeof(ContractContext), "contractContext");
        var patchedCondition = WithPatchedValueAtReturn(WithPatchedOldValues(WithPatchedResults(condition, contractContextParam), contractContextParam), contractContextParam);

        var postconditionPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Ensures), new Type[] { typeof(bool), typeof(string), typeof(Type) })!;

        var postconditionParams = new List<ParameterExpression>(_contractParameters!)
                {
                    contractContextParam
                };

        var contract = Expression.Lambda(Expression.Call(null, postconditionPatch, patchedCondition, message, exceptionType), $"Ensures_1", postconditionParams);
        contractDlg = contract.Compile();
    }

    private void GetEnsuresOnThrowDelegate(MethodCallExpression contractEnsuresOnThrowExpr, out Delegate contractDlg)
    {
        var condition = contractEnsuresOnThrowExpr.Arguments[0];
        var message = contractEnsuresOnThrowExpr.Arguments.Count > 1 ? contractEnsuresOnThrowExpr.Arguments[1] : Expression.Constant(null, typeof(string));

        MethodInfo ensuresOnThrowPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.EnsuresOnThrow), 1, new Type[] { typeof(bool), typeof(string) })!;

        var exceptionTypeArg = contractEnsuresOnThrowExpr.Method.GetGenericArguments()[0];

        var ensuresOnThrowParams = new List<ParameterExpression>(_contractParameters!);

        var contract = Expression.Lambda(
            Expression.Call(
                null,
                ensuresOnThrowPatch.MakeGenericMethod(exceptionTypeArg),
                condition,
                message),
            $"EnsuresOnThrow_1",
            ensuresOnThrowParams);
        contractDlg = contract.Compile();
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {

        if (node.Method.DeclaringType == typeof(Contract))
        {
            switch (node.Method.Name)
            {
                case nameof(Contract.Requires):
                    GetPreconditionDelegate(node, out var preconditionDelegate);
                    Preconditions.Add(preconditionDelegate);
                    break;
                case nameof(Contract.Ensures):
                    GetPostconditionDelegate(node, out var postconditionDelegate, out var oldValueCollectorDelegate);
                    Postconditions.Add(postconditionDelegate);
                    foreach (var property in oldValueCollectorDelegate.Keys)
                    {
                        if (!OldValueCollectors.ContainsKey(property))
                        {
                            OldValueCollectors.Add(property, oldValueCollectorDelegate[property]);
                        }
                    }
                    break;

                case nameof(Contract.EnsuresOnThrow):
                    GetEnsuresOnThrowDelegate(node, out var ensuresOnThrowDelegate);
                    PostconditionsOnThrow.Add(ensuresOnThrowDelegate);
                    break;
                case nameof(Contract.Invariant):
                    break;
                default:
                    return base.VisitMethodCall(node);
            }
        }

        return base.VisitMethodCall(node);
    }


}
