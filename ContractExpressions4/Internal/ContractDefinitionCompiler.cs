using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractExpressions4.Internal;

internal static class ContractDefinitionCompiler
{
    private static readonly MethodInfo GetTargetMethod = typeof(ContractInvocationContext).GetMethod(nameof(ContractInvocationContext.GetTarget))!;
    private static readonly MethodInfo GetArgumentMethod = typeof(ContractInvocationContext).GetMethod(nameof(ContractInvocationContext.GetArgument))!;
    private static readonly MethodInfo GetResultMethod = typeof(ContractInvocationContext).GetMethod(nameof(ContractInvocationContext.GetResult))!;
    private static readonly MethodInfo GetOldValueMethod = typeof(ContractInvocationContext).GetMethod(nameof(ContractInvocationContext.GetOldValue))!;

    public static CompiledContract Compile(LambdaExpression clause)
    {
        if (clause.Body is not MethodCallExpression call || call.Method.DeclaringType != typeof(Contract))
        {
            throw new InvalidOperationException($"'{clause}' is not a valid contract definition.");
        }

        ContractKind kind = call.Method.Name switch
        {
            nameof(Contract.Requires) => ContractKind.Precondition,
            nameof(Contract.Ensures) => ContractKind.Postcondition,
            nameof(Contract.Invariant) => ContractKind.Invariant,
            _ => throw new InvalidOperationException($"Unsupported contract method '{call.Method.Name}'.")
        };

        if (call.Arguments.Count == 0)
        {
            throw new InvalidOperationException($"Contract clause '{clause}' does not include a boolean predicate.");
        }

        string token = Guid.NewGuid().ToString("N");
        ParameterExpression contextParameter = Expression.Parameter(typeof(ContractInvocationContext), "ctx");
        var captures = ImmutableArray.CreateBuilder<OldValueCapture>();

        Expression boolBody = new ContractPredicateRewriter(clause.Parameters, contextParameter, token, captures).Visit(call.Arguments[0])
            ?? throw new InvalidOperationException($"Failed to rewrite contract clause '{clause}'.");

        Expression<Func<ContractInvocationContext, bool>> lambda = Expression.Lambda<Func<ContractInvocationContext, bool>>(boolBody, contextParameter);

        return new CompiledContract(kind, clause.ToString(), token, lambda.Compile(), captures.ToImmutable());
    }

    private sealed class ContractPredicateRewriter(
        IReadOnlyList<ParameterExpression> parameters,
        ParameterExpression contextParameter,
        string token,
        ImmutableArray<OldValueCapture>.Builder captures)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            int index = FindParameterIndex(node);
            if (index < 0)
            {
                return base.VisitParameter(node);
            }

            if (index == 0)
            {
                return Expression.Call(contextParameter, GetTargetMethod.MakeGenericMethod(node.Type));
            }

            return Expression.Call(
                contextParameter,
                GetArgumentMethod.MakeGenericMethod(node.Type),
                Expression.Constant(index - 1));
        }

        private int FindParameterIndex(ParameterExpression parameter)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (ReferenceEquals(parameters[i], parameter))
                {
                    return i;
                }
            }

            return -1;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType != typeof(Contract))
            {
                return base.VisitMethodCall(node);
            }

            if (node.Method.Name == nameof(Contract.Result))
            {
                Type resultType = node.Method.GetGenericArguments()[0];
                return Expression.Call(contextParameter, GetResultMethod.MakeGenericMethod(resultType));
            }

            if (node.Method.Name == nameof(Contract.ValueAtReturn))
            {
                return Visit(node.Arguments[0]);
            }

            if (node.Method.Name == nameof(Contract.OldValue))
            {
                Type valueType = node.Method.GetGenericArguments()[0];
                int slot = captures.Count;

                Expression oldExpression = node.Arguments[0];
                Expression oldBody = new OldValueCaptureRewriter(parameters, contextParameter).Visit(oldExpression)
                    ?? throw new InvalidOperationException("Failed to rewrite an old-value expression.");

                Expression<Func<ContractInvocationContext, object?>> captureLambda = Expression.Lambda<Func<ContractInvocationContext, object?>>(
                    Expression.Convert(oldBody, typeof(object)),
                    contextParameter);

                captures.Add(new OldValueCapture(slot, captureLambda.Compile()));

                return Expression.Call(
                    contextParameter,
                    GetOldValueMethod.MakeGenericMethod(valueType),
                    Expression.Constant(token),
                    Expression.Constant(slot));
            }

            return base.VisitMethodCall(node);
        }
    }

    private sealed class OldValueCaptureRewriter(IReadOnlyList<ParameterExpression> parameters, ParameterExpression contextParameter)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            int index = FindParameterIndex(node);
            if (index < 0)
            {
                return base.VisitParameter(node);
            }

            if (index == 0)
            {
                return Expression.Call(contextParameter, GetTargetMethod.MakeGenericMethod(node.Type));
            }

            return Expression.Call(
                contextParameter,
                GetArgumentMethod.MakeGenericMethod(node.Type),
                Expression.Constant(index - 1));
        }

        private int FindParameterIndex(ParameterExpression parameter)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (ReferenceEquals(parameters[i], parameter))
                {
                    return i;
                }
            }

            return -1;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType != typeof(Contract))
            {
                return base.VisitMethodCall(node);
            }

            if (node.Method.Name == nameof(Contract.ValueAtReturn))
            {
                return Visit(node.Arguments[0]);
            }

            if (node.Method.Name is nameof(Contract.OldValue) or nameof(Contract.Result))
            {
                throw new InvalidOperationException("Nested Contract.OldValue and Contract.Result are not supported inside old-value capture.");
            }

            return base.VisitMethodCall(node);
        }
    }
}
