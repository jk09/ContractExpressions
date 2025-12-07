using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

internal class DbcDefVisitor(Type contractType) : ExpressionVisitor
{
    private IList<ParameterExpression>? _contractParameters;
    private readonly Type _contractType = contractType;
    public IList<Delegate> Preconditions { get; } = new List<Delegate>();
    public IList<Delegate> Postconditions { get; } = new List<Delegate>();
    public IList<Delegate> PostconditionsOnThrow { get; } = new List<Delegate>();
    public IList<Delegate> Invariants { get; } = new List<Delegate>();
    public IDictionary<PropertyInfo, Delegate> OldValueCollectors { get; } = new Dictionary<PropertyInfo, Delegate>();

    public override Expression? Visit(Expression? node)
    {
        if (_contractParameters == null && node is LambdaExpression lambda)
        {
            _contractParameters = lambda.Parameters;
        }

        return base.Visit(node);
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
                    GetPostconditionDelegate(node, out var postconditionDelegate, out var oldValueCollectorDelegates);
                    Postconditions.Add(postconditionDelegate);
                    foreach (var property in oldValueCollectorDelegates.Keys)
                    {
                        if (!OldValueCollectors.ContainsKey(property))
                        {
                            OldValueCollectors.Add(property, oldValueCollectorDelegates[property]);
                        }
                    }
                    break;

                case nameof(Contract.EnsuresOnThrow):
                    GetEnsuresOnThrowDelegate(node, out var ensuresOnThrowDelegate);
                    PostconditionsOnThrow.Add(ensuresOnThrowDelegate);
                    break;
                case nameof(Contract.Assert):
                    var assertCallExpr = new ContractAssertPatcher().Visit(node);
                    return assertCallExpr!;
                case nameof(Contract.Assume):
                    var assumeCallExpr = new ContractAssumePatcher().Visit(node);
                    return assumeCallExpr!;

                case nameof(Contract.Invariant):
                    var invariantCallExpr = node;
                    GetInvariantDelegate(invariantCallExpr, out var invariantDelegate);
                    Invariants.Add(invariantDelegate);
                    break;
            }
        }

        return base.VisitMethodCall(node);
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
            exceptionType = Expression.Constant(exceptionTypeArg, typeof(Type)); // message already extracted
        }
        else
        {
            exceptionType = Expression.Constant(null, typeof(Type));
        }

        var preconditionParams = new List<ParameterExpression>(_contractParameters!);

        Expression callExpr = Expression.Call(null, preconditionPatch, condition, message, exceptionType);

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

        var condition1 = new ContractResultPatcher(contractContextParam).Visit(condition);
        var condition2 = new ContractOldValuePatcher(contractContextParam).Visit(condition1);
        var condition3 = new ContractValueAtReturnPatcher(contractContextParam).Visit(condition2);

        var patchedCondition = condition3;

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

    private void GetInvariantDelegate(MethodCallExpression contractInvariantExpr, out Delegate contractDlg)
    {
        var condition = contractInvariantExpr.Arguments[0];
        var message = contractInvariantExpr.Arguments.Count > 1 ? contractInvariantExpr.Arguments[1] : Expression.Constant(null, typeof(string));

        MethodInfo invariantPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Invariant), new Type[] { typeof(bool), typeof(string) })!;

        var invariantParams = new List<ParameterExpression>(_contractParameters!);

        var contract = Expression.Lambda(Expression.Call(null, invariantPatch, condition, message), $"Invariant_1", invariantParams);
        contractDlg = contract.Compile();
    }
}


