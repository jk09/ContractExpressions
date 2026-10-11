#define CONTRACTS_FULL

using System.Diagnostics.Contracts;
using System.Reflection;

namespace ContractCheck.Tests.Definitions.Compilation;

/// <summary>
/// Resolution R21: clauses are compiled to delegates once, when they are defined, and every call reuses them
/// (ContractExpressions ran <c>DynamicInvoke</c> per clause and call).
/// </summary>
public class CompilationTests
{
    [Fact]
    public void Clauses_are_compiled_once_when_defined_and_reused_by_every_call()
    {
        MethodInfo apply = typeof(ICompiledOnce).GetMethod(nameof(ICompiledOnce.Apply))!;
        Dbc.Def(static (ICompiledOnce x, int value) => x.Apply(value), static (ICompiledOnce x, int value) => Contract.Requires(value > 0));
        Func<InvocationContext, bool> compiled = ContractRegistry.For(typeof(ICompiledOnce)).ClausesFor(apply).Single().Predicate;

        Dbc.Make<ICompiledOnce>(new CompiledOnce()).Apply(1);
        Dbc.Make<ICompiledOnce>(new CompiledOnce()).Apply(2);
        Expect.Violation(ContractFailureKind.Precondition, () => Dbc.Make<ICompiledOnce>(new CompiledOnce()).Apply(0));

        Assert.Same(compiled, ContractRegistry.For(typeof(ICompiledOnce)).ClausesFor(apply).Single().Predicate);
    }
}

public interface ICompiledOnce
{
    void Apply(int value);
}

public sealed class CompiledOnce : ICompiledOnce
{
    public void Apply(int value)
    {
    }
}
