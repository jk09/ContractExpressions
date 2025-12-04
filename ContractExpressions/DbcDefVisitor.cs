using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions;

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

    private static Expression WithPatchedValueAtReturn(Expression condition, ParameterExpression contractContextParam)
    {
        var valueAtReturnPatcher = new ContractValueAtReturnPatchVisitor(contractContextParam);
        var patched = valueAtReturnPatcher.Visit(condition);
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

                MethodInfo preconditionPatch;
                Expression exceptionType;

                if (node.Method.GetGenericArguments().Length > 0)
                {
                    // Requires<TException>(bool, string)
                    var exceptionTypeArg = node.Method.GetGenericArguments()[0];
                    preconditionPatch = typeof(ContractPatch)
                        .GetMethod(nameof(ContractPatch.Requires), 1, new Type[] { typeof(bool), typeof(string) })!
                        .MakeGenericMethod(exceptionTypeArg);
                    exceptionType = Expression.Constant(null, typeof(string)); // message already extracted
                }
                else
                {
                    // Requires(bool, string, Type)
                    preconditionPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Requires), new Type[] { typeof(bool), typeof(string), typeof(Type) })!;
                    exceptionType = Expression.Constant(null, typeof(Type));
                }

                var preconditionParams = new List<ParameterExpression>(_contractParameters!);

                Expression callExpr = node.Method.GetGenericArguments().Length > 0
                    ? Expression.Call(null, preconditionPatch, condition, message)
                    : Expression.Call(null, preconditionPatch, condition, message, exceptionType);

                var contract = Expression.Lambda(callExpr, $"Requires_1", preconditionParams);
                var contractDlg = contract.Compile();
                Preconditions.Add(contractDlg);
            }
            else if (node.Method.Name == nameof(Contract.Ensures))
            {
                var condition = node.Arguments[0];
                var message = node.Arguments.Count > 1 ? node.Arguments[1] : Expression.Constant(null, typeof(string));
                var exceptionType = Expression.Constant(null, typeof(Type));

                AddOldValueCollectors(condition);

                var contractContextParam = Expression.Parameter(typeof(ContractContext), "contractContext");
                var patchedCondition = WithPatchedValueAtReturn(WithPatchedOldValues(WithPatchedResults(condition, contractContextParam), contractContextParam), contractContextParam);

                var postconditionPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Ensures), new Type[] { typeof(bool), typeof(string), typeof(Type) })!;

                var postconditionParams = new List<ParameterExpression>(_contractParameters!)
                {
                    contractContextParam
                };

                var contract = Expression.Lambda(Expression.Call(null, postconditionPatch, patchedCondition, message, exceptionType), $"Ensures_1", postconditionParams);
                var contractDlg = contract.Compile();
                Postconditions.Add(contractDlg);
            }
            else if (node.Method.Name == nameof(Contract.EnsuresOnThrow))
            {
                var condition = node.Arguments[0];
                var message = node.Arguments.Count > 1 ? node.Arguments[1] : Expression.Constant(null, typeof(string));

                var exceptionTypeArg = node.Method.GetGenericArguments()[0];
                var postconditionPatch = typeof(ContractPatch)
                    .GetMethod(nameof(ContractPatch.EnsuresOnThrow), 1, new Type[] { typeof(bool), typeof(string) })!
                    .MakeGenericMethod(exceptionTypeArg);

                AddOldValueCollectors(condition);

                var contractContextParam = Expression.Parameter(typeof(ContractContext), "contractContext");
                var patchedCondition = WithPatchedValueAtReturn(WithPatchedOldValues(WithPatchedResults(condition, contractContextParam), contractContextParam), contractContextParam);

                var postconditionParams = new List<ParameterExpression>(_contractParameters!)
                {
                    contractContextParam
                };

                var contract = Expression.Lambda(Expression.Call(null, postconditionPatch, patchedCondition, message), $"EnsuresOnThrow_1", postconditionParams);
                var contractDlg = contract.Compile();
                Postconditions.Add(contractDlg);
            }
            else if (node.Method.Name == nameof(Contract.Assert))
            {
                var condition = node.Arguments[0];
                var message = node.Arguments.Count > 1 ? node.Arguments[1] : Expression.Constant(null, typeof(string));

                var assertPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Assert), new Type[] { typeof(bool), typeof(string) })!;
                var preconditionParams = new List<ParameterExpression>(_contractParameters!);
                var contract = Expression.Lambda(Expression.Call(null, assertPatch, condition, message), $"Assert_1", preconditionParams);
                var contractDlg = contract.Compile();
                Preconditions.Add(contractDlg);
            }
            else if (node.Method.Name == nameof(Contract.Assume))
            {
                var condition = node.Arguments[0];
                var message = node.Arguments.Count > 1 ? node.Arguments[1] : Expression.Constant(null, typeof(string));

                var assumePatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Assume), new Type[] { typeof(bool), typeof(string) })!;
                var preconditionParams = new List<ParameterExpression>(_contractParameters!);
                var contract = Expression.Lambda(Expression.Call(null, assumePatch, condition, message), $"Assume_1", preconditionParams);
                var contractDlg = contract.Compile();
                Preconditions.Add(contractDlg);
            }
            else if (node.Method.Name == nameof(Contract.Invariant))
            {
                var condition = node.Arguments[0];
                var message = node.Arguments.Count > 1 ? node.Arguments[1] : Expression.Constant(null, typeof(string));

                var invariantPatch = typeof(ContractPatch).GetMethod(nameof(ContractPatch.Invariant), new Type[] { typeof(bool), typeof(string) })!;
                var preconditionParams = new List<ParameterExpression>(_contractParameters!);
                var contract = Expression.Lambda(Expression.Call(null, invariantPatch, condition, message), $"Invariant_1", preconditionParams);
                var contractDlg = contract.Compile();
                Preconditions.Add(contractDlg);
            }
        }

        return base.VisitMethodCall(node);
    }


}
