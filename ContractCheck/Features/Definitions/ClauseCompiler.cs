using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractCheck;

/// <summary>
/// Compiles one <c>Contract.*</c> clause lambda into a <see cref="CompiledClause"/>: validates where <c>Result</c> and
/// <c>OldValue</c> may appear, rewrites lambda parameters into <see cref="InvocationContext"/> accessors, and turns every
/// <c>OldValue</c> into a capture made before the call.
/// </summary>
internal static class ClauseCompiler
{
    private static readonly MethodInfo GetTargetMethod = ContextMethod(nameof(InvocationContext.GetTarget));
    private static readonly MethodInfo GetArgumentMethod = ContextMethod(nameof(InvocationContext.GetArgument));
    private static readonly MethodInfo GetResultMethod = ContextMethod(nameof(InvocationContext.GetResult));
    private static readonly MethodInfo GetOldValueMethod = ContextMethod(nameof(InvocationContext.GetOldValue));
    private static readonly MethodInfo GetOldValueAtMethod = ContextMethod(nameof(InvocationContext.GetOldValueAt));

    /// <summary>Returns the <c>Contract.*</c> call that forms <paramref name="body"/>, if it is one.</summary>
    public static MethodCallExpression? AsContractCall(Expression body) =>
        body is MethodCallExpression call && call.Method.DeclaringType == typeof(Contract) ? call : null;

    /// <summary>Compiles a clause attached to <paramref name="member"/>, or an interface invariant when <paramref name="member"/> is null.</summary>
    public static CompiledClause Compile(LambdaExpression clause, Type interfaceType, MemberSignature? member)
    {
        MethodCallExpression call = AsContractCall(clause.Body)
            ?? throw DefinitionErrors.Create(interfaceType, clause, "a contract clause must be a single call to System.Diagnostics.Contracts.Contract, e.g. Contract.Requires(...)");

        ClauseKind kind = KindOf(call, interfaceType, clause);

        int expectedParameters = member?.ClauseParameterCount ?? 1;
        if (clause.Parameters.Count != expectedParameters)
        {
            throw DefinitionErrors.Create(interfaceType, clause, $"the clause must take the same {expectedParameters} parameter(s) as the member selector");
        }

        if (member is null && kind != ClauseKind.Invariant)
        {
            throw DefinitionErrors.Create(
                interfaceType,
                clause,
                "an interface-level Dbc.Def accepts only Contract.Invariant clauses; attach other clauses to a member, e.g. Dbc.Def((IFoo x) => x.M(), (IFoo x) => Contract.Ensures(...))");
        }

        Type? exceptionType = kind is ClauseKind.RequiresOfException or ClauseKind.EnsuresOnThrow ? call.Method.GetGenericArguments()[0] : null;
        if (kind == ClauseKind.RequiresOfException && TypedExceptionFactory.WhyNotConstructible(exceptionType!) is { } reason)
        {
            throw DefinitionErrors.Create(interfaceType, clause, $"Contract.Requires<{TypeNames.Format(exceptionType!)}> cannot be thrown: {reason}");
        }

        var rewriter = new ClauseRewriter(clause, kind, interfaceType, member);
        Expression condition = rewriter.Rewrite(call.Arguments[0]);
        Func<InvocationContext, bool> predicate =
            Expression.Lambda<Func<InvocationContext, bool>>(condition, rewriter.Context).Compile();

        Func<InvocationContext, string?>? message = null;
        if (call.Arguments.Count > 1)
        {
            if (call.Arguments[1] is ConstantExpression { Value: var text })
            {
                string? constant = (string?)text;
                message = _ => constant;
            }
            else
            {
                message = Expression.Lambda<Func<InvocationContext, string?>>(rewriter.Rewrite(call.Arguments[1]), rewriter.Context).Compile();
            }
        }

        if (rewriter.UsesTaskResult && rewriter.UsesAwaitedResult)
        {
            throw DefinitionErrors.Create(interfaceType, clause, "a clause cannot read both the returned task and its awaited result");
        }

        bool deferred = member is { IsAwaitable: true }
            && (kind == ClauseKind.Invariant || (kind == ClauseKind.Ensures && !rewriter.UsesTaskResult));

        return new CompiledClause
        {
            Kind = kind,
            Predicate = predicate,
            Message = message,
            ConditionText = ExpressionPrinter.Print(call.Arguments[0]),
            ContractText = ExpressionPrinter.Print(call),
            ExceptionType = exceptionType,
            OldValueCaptures = rewriter.Captures.ToImmutableArray(),
            Deferred = deferred,
            RuntimeIgnored = rewriter.RuntimeIgnored,
        };
    }

    private static ClauseKind KindOf(MethodCallExpression call, Type interfaceType, LambdaExpression clause) => call.Method.Name switch
    {
        nameof(Contract.Requires) => call.Method.IsGenericMethod ? ClauseKind.RequiresOfException : ClauseKind.Requires,
        nameof(Contract.Ensures) => ClauseKind.Ensures,
        nameof(Contract.EnsuresOnThrow) => ClauseKind.EnsuresOnThrow,
        nameof(Contract.Invariant) => ClauseKind.Invariant,
        nameof(Contract.Assert) => ClauseKind.Assert,
        nameof(Contract.Assume) => ClauseKind.Assume,
        nameof(Contract.EndContractBlock) => throw DefinitionErrors.Create(
            interfaceType,
            clause,
            "Contract.EndContractBlock marks legacy if-then-throw preconditions inside method bodies; use Contract.Requires<TException> instead"),
        nameof(Contract.ValueAtReturn) => throw ValueAtReturnError(interfaceType, clause),
        _ => throw DefinitionErrors.Create(
            interfaceType,
            clause,
            $"Contract.{call.Method.Name} is not a clause; use it inside a condition, e.g. Contract.Ensures(Contract.{call.Method.Name}(...))"),
    };

    private static InvalidOperationException ValueAtReturnError(Type interfaceType, Expression expression) =>
        DefinitionErrors.Create(
            interfaceType,
            expression,
            "Contract.ValueAtReturn needs out parameters, which Dbc.Def selectors cannot express; contracts on ref/out parameters are not supported");

    private static MethodInfo ContextMethod(string name) =>
        typeof(InvocationContext).GetMethod(name) ?? throw new MissingMethodException(nameof(InvocationContext), name);

    /// <summary>Rewrites one clause; holds the old-value captures and the flags the compiled clause needs.</summary>
    private sealed class ClauseRewriter(LambdaExpression clause, ClauseKind kind, Type interfaceType, MemberSignature? member) : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, (Expression From, Expression To)> quantifierRanges = [];

        public ParameterExpression Context { get; } = Expression.Parameter(typeof(InvocationContext), "context");

        public List<Func<InvocationContext, object?>> Captures { get; } = [];

        public bool UsesTaskResult { get; private set; }

        public bool UsesAwaitedResult { get; private set; }

        public bool RuntimeIgnored { get; private set; }

        public Expression Rewrite(Expression expression) => Visit(expression)!;

        protected override Expression VisitParameter(ParameterExpression node) =>
            ClauseParameters.Map(clause.Parameters, node, Context) ?? node;

        protected override Expression VisitMember(MemberExpression node)
        {
            MarkRuntimeIgnored(node.Member);
            return base.VisitMember(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Constructor is not null)
            {
                MarkRuntimeIgnored(node.Constructor);
            }

            return base.VisitNew(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            MarkRuntimeIgnored(node.Method);
            if (node.Method.DeclaringType == typeof(Contract))
            {
                switch (node.Method.Name)
                {
                    case nameof(Contract.Result):
                        return RewriteResult(node);
                    case nameof(Contract.OldValue):
                        return RewriteOldValue(node);
                    case nameof(Contract.ValueAtReturn):
                        throw ValueAtReturnError(interfaceType, clause);
                    case nameof(Contract.ForAll) or nameof(Contract.Exists)
                        when node.Arguments.Count == 3 && node.Arguments[2] is LambdaExpression { Parameters.Count: 1 } predicate:
                        quantifierRanges[predicate.Parameters[0]] = (node.Arguments[0], node.Arguments[1]);
                        break;
                }
            }

            return base.VisitMethodCall(node);
        }

        private Expression RewriteResult(MethodCallExpression node)
        {
            Type requested = node.Method.GetGenericArguments()[0];
            if (kind != ClauseKind.Ensures || member is null)
            {
                throw Error(node, kind == ClauseKind.EnsuresOnThrow
                    ? "Contract.Result cannot be used in EnsuresOnThrow: the member threw instead of returning (CodeContracts error CC1001)"
                    : "Contract.Result can only be used in Ensures clauses (CodeContracts error CC1023)");
            }

            if (member.ReturnType == typeof(void))
            {
                throw Error(node, $"{member.DisplayName} returns void, so Contract.Result<{TypeNames.Format(requested)}>() has no value");
            }

            if (requested == member.ReturnType)
            {
                UsesTaskResult |= member.IsAwaitable;
            }
            else if (requested == member.AwaitedType)
            {
                UsesAwaitedResult = true;
            }
            else
            {
                string expected = member.AwaitedType is null
                    ? TypeNames.Format(member.ReturnType)
                    : $"{TypeNames.Format(member.AwaitedType)} (the awaited result) or {TypeNames.Format(member.ReturnType)}";
                throw Error(node, $"Contract.Result<{TypeNames.Format(requested)}>() does not match the return type of {member.DisplayName}; use Contract.Result<{expected}>() (CodeContracts error CC1002)");
            }

            return Expression.Call(Context, GetResultMethod.MakeGenericMethod(requested));
        }

        private Expression RewriteOldValue(MethodCallExpression node)
        {
            if (kind is not (ClauseKind.Ensures or ClauseKind.EnsuresOnThrow))
            {
                throw Error(node, "Contract.OldValue can only be used in Ensures and EnsuresOnThrow clauses (CodeContracts error CC1023)");
            }

            Expression value = node.Arguments[0];
            if (StripConversions(value) is ConstantExpression)
            {
                throw Error(node, "Contract.OldValue of a literal is always the literal (CodeContracts error CC1000)");
            }

            if (NestedOldValueOrResultFinder.Contains(value))
            {
                throw Error(node, "Contract.OldValue cannot contain Contract.Result or another Contract.OldValue");
            }

            Type valueType = node.Method.GetGenericArguments()[0];
            IReadOnlyCollection<ParameterExpression> boundVariables = FreeVariableFinder.Find(value, clause.Parameters);
            if (boundVariables.Count == 0)
            {
                Expression captured = Expression.Convert(ClauseParameters.Rewrite(clause.Parameters, value, Context), typeof(object));
                Captures.Add(Expression.Lambda<Func<InvocationContext, object?>>(captured, Context).Compile());
                return Expression.Call(Context, GetOldValueMethod.MakeGenericMethod(valueType), Expression.Constant(Captures.Count - 1));
            }

            if (boundVariables.Count == 1
                && boundVariables.Single() is var index
                && quantifierRanges.TryGetValue(index, out var range)
                && FreeVariableFinder.Find(range.From, clause.Parameters).Count == 0
                && FreeVariableFinder.Find(range.To, clause.Parameters).Count == 0
                && !NestedOldValueOrResultFinder.Contains(range.From)
                && !NestedOldValueOrResultFinder.Contains(range.To))
            {
                Func<InvocationContext, int> from = CompileAtEntry<int>(range.From);
                Func<InvocationContext, int> to = CompileAtEntry<int>(range.To);
                Func<InvocationContext, int, object?> element = Expression.Lambda<Func<InvocationContext, int, object?>>(
                    Expression.Convert(ClauseParameters.Rewrite(clause.Parameters, value, Context), typeof(object)),
                    Context,
                    index).Compile();

                Captures.Add(context =>
                {
                    int low = from(context);
                    var values = new object?[Math.Max(0, to(context) - low)];
                    for (int offset = 0; offset < values.Length; offset++)
                    {
                        values[offset] = element(context, low + offset);
                    }

                    return new QuantifiedOldValues(low, values);
                });

                return Expression.Call(Context, GetOldValueAtMethod.MakeGenericMethod(valueType), Expression.Constant(Captures.Count - 1), index);
            }

            throw Error(
                node,
                "Contract.OldValue can only refer to the variable of an enclosing Contract.ForAll/Exists(from, to, i => ...) whose bounds do not depend on the result; " +
                "for other lambdas, capture a snapshot instead, e.g. Contract.OldValue(xs.ToArray())");
        }

        private Func<InvocationContext, T> CompileAtEntry<T>(Expression expression) =>
            Expression.Lambda<Func<InvocationContext, T>>(ClauseParameters.Rewrite(clause.Parameters, expression, Context), Context).Compile();

        private void MarkRuntimeIgnored(MemberInfo memberInfo)
        {
            if (memberInfo.IsDefined(typeof(ContractRuntimeIgnoredAttribute), inherit: true)
                || memberInfo.DeclaringType?.IsDefined(typeof(ContractRuntimeIgnoredAttribute), inherit: true) == true)
            {
                RuntimeIgnored = true;
            }
        }

        private InvalidOperationException Error(Expression at, string reason) =>
            DefinitionErrors.Create(interfaceType, clause, $"{ExpressionPrinter.Print(at)}: {reason}");

        private static Expression StripConversions(Expression expression)
        {
            while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
            {
                expression = conversion.Operand;
            }

            return expression;
        }
    }

    /// <summary>Maps clause lambda parameters to <see cref="InvocationContext"/> accessors.</summary>
    private static class ClauseParameters
    {
        public static Expression? Map(IReadOnlyList<ParameterExpression> parameters, ParameterExpression node, ParameterExpression context)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] == node)
                {
                    return i == 0
                        ? Expression.Call(context, GetTargetMethod.MakeGenericMethod(node.Type))
                        : Expression.Call(context, GetArgumentMethod.MakeGenericMethod(node.Type), Expression.Constant(i - 1));
                }
            }

            return null;
        }

        /// <summary>Rewrites only the clause parameters of an expression that contains no <c>Result</c>/<c>OldValue</c>.</summary>
        public static Expression Rewrite(IReadOnlyList<ParameterExpression> parameters, Expression expression, ParameterExpression context) =>
            new ParameterOnlyRewriter(parameters, context).Visit(expression)!;

        private sealed class ParameterOnlyRewriter(IReadOnlyList<ParameterExpression> parameters, ParameterExpression context) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) => Map(parameters, node, context) ?? node;
        }
    }

    /// <summary>Finds <c>Contract.Result</c> or <c>Contract.OldValue</c> calls inside an expression.</summary>
    private sealed class NestedOldValueOrResultFinder : ExpressionVisitor
    {
        private bool found;

        public static bool Contains(Expression expression)
        {
            var finder = new NestedOldValueOrResultFinder();
            finder.Visit(expression);
            return finder.found;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Contract) && node.Method.Name is nameof(Contract.Result) or nameof(Contract.OldValue))
            {
                found = true;
                return node;
            }

            return base.VisitMethodCall(node);
        }
    }

    /// <summary>Finds the lambda parameters an expression uses but neither declares nor receives from the clause.</summary>
    private sealed class FreeVariableFinder(IReadOnlyList<ParameterExpression> clauseParameters) : ExpressionVisitor
    {
        private readonly HashSet<ParameterExpression> declared = [];
        private readonly HashSet<ParameterExpression> free = [];

        public static IReadOnlyCollection<ParameterExpression> Find(Expression expression, IReadOnlyList<ParameterExpression> clauseParameters)
        {
            var finder = new FreeVariableFinder(clauseParameters);
            finder.Visit(expression);
            return finder.free;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            declared.UnionWith(node.Parameters);
            return base.VisitLambda(node);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            declared.UnionWith(node.Variables);
            return base.VisitBlock(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (!declared.Contains(node) && !clauseParameters.Contains(node))
            {
                free.Add(node);
            }

            return node;
        }
    }
}
