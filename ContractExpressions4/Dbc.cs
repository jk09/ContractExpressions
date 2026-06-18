using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using ContractExpressions4.Internal;

namespace ContractExpressions4;

public static class Dbc
{
    public static void Def<T>(Expression<Action<T>> method, params Expression<Action<T>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1>(Expression<Action<T, T1>> method, params Expression<Action<T, T1>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2>(Expression<Action<T, T1, T2>> method, params Expression<Action<T, T1, T2>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2, T3>(Expression<Action<T, T1, T2, T3>> method, params Expression<Action<T, T1, T2, T3>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2, T3, T4>(Expression<Action<T, T1, T2, T3, T4>> method, params Expression<Action<T, T1, T2, T3, T4>>[] contracts) => Register(method, contracts); public static void Def<T, T1, T2, T3, T4, T5>(Expression<Action<T, T1, T2, T3, T4, T5>> method, params Expression<Action<T, T1, T2, T3, T4, T5>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2, T3, T4, T5, T6>(Expression<Action<T, T1, T2, T3, T4, T5, T6>> method, params Expression<Action<T, T1, T2, T3, T4, T5, T6>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7>(Expression<Action<T, T1, T2, T3, T4, T5, T6, T7>> method, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7>>[] contracts) => Register(method, contracts);

    public static void Def<T, TResult>(Expression<Func<T, TResult>> method, params Expression<Action<T>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, TResult>(Expression<Func<T, T1, TResult>> method, params Expression<Action<T, T1>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2, TResult>(Expression<Func<T, T1, T2, TResult>> method, params Expression<Action<T, T1, T2>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2, T3, TResult>(Expression<Func<T, T1, T2, T3, TResult>> method, params Expression<Action<T, T1, T2, T3>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2, T3, T4, TResult>(Expression<Func<T, T1, T2, T3, T4, TResult>> method, params Expression<Action<T, T1, T2, T3, T4>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2, T3, T4, T5, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, TResult>> method, params Expression<Action<T, T1, T2, T3, T4, T5>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, TResult>> method, params Expression<Action<T, T1, T2, T3, T4, T5, T6>>[] contracts) => Register(method, contracts);
    public static void Def<T, T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T, T1, T2, T3, T4, T5, T6, T7, TResult>> method, params Expression<Action<T, T1, T2, T3, T4, T5, T6, T7>>[] contracts) => Register(method, contracts);

    public static T Make<T>(T target)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(target);

        Type interfaceType = typeof(T);
        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException($"'{interfaceType.FullName}' must be an interface type.");
        }

        ContractClassLoader.EnsureLoaded(interfaceType);
        InterfaceContracts interfaceContracts = ContractRegistry.Instance.Get(interfaceType);

        T proxy = DispatchProxy.Create<T, ContractAwareProxy<T>>();
        ContractAwareProxy<T> runtimeProxy = (ContractAwareProxy<T>)(object)proxy;
        runtimeProxy.Initialize(target, interfaceContracts);
        runtimeProxy.ValidateInvariantsAfterCreation();
        return proxy;
    }

    private static void Register(LambdaExpression methodSelector, IReadOnlyList<LambdaExpression> contractClauses)
    {
        ArgumentNullException.ThrowIfNull(methodSelector);
        ArgumentNullException.ThrowIfNull(contractClauses);

        Type interfaceType = methodSelector.Parameters.FirstOrDefault()?.Type
            ?? throw new InvalidOperationException("Method selector must include the target interface parameter.");

        if (!interfaceType.IsInterface)
        {
            throw new InvalidOperationException($"'{interfaceType.FullName}' is not an interface type.");
        }

        if (contractClauses.Count == 0)
        {
            if (IsContractClause(methodSelector))
            {
                CompiledContract contract = ContractDefinitionCompiler.Compile(methodSelector);
                if (contract.Kind != ContractKind.Invariant)
                {
                    throw new InvalidOperationException("Single-argument Dbc.Def is reserved for invariant definitions.");
                }

                ContractRegistry.Instance.AddInvariant(interfaceType, contract);
                return;
            }

            throw new InvalidOperationException("At least one contract clause must be provided.");
        }

        MethodInfo method = ExtractMethod(methodSelector);
        List<CompiledContract> compiled = [];
        foreach (LambdaExpression clause in contractClauses)
        {
            if (clause.Parameters.Count != methodSelector.Parameters.Count)
            {
                throw new InvalidOperationException($"Contract clause '{clause}' does not match method parameters.");
            }

            CompiledContract compiledContract = ContractDefinitionCompiler.Compile(clause);
            if (compiledContract.Kind == ContractKind.Invariant)
            {
                ContractRegistry.Instance.AddInvariant(interfaceType, compiledContract);
            }
            else
            {
                compiled.Add(compiledContract);
            }
        }

        if (compiled.Count > 0)
        {
            ContractRegistry.Instance.AddMethodContracts(interfaceType, method, compiled);
        }
    }

    private static bool IsContractClause(LambdaExpression expression) =>
        expression.Body is MethodCallExpression call && call.Method.DeclaringType == typeof(Contract);

    private static MethodInfo ExtractMethod(LambdaExpression selector)
    {
        if (selector.Body is MethodCallExpression call && call.Method.DeclaringType != typeof(Contract))
        {
            return call.Method;
        }

        throw new InvalidOperationException($"'{selector}' is not a valid method selector.");
    }
}
