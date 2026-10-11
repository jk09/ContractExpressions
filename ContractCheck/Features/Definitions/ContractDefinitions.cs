using System.Diagnostics.Contracts;
using System.Linq.Expressions;

namespace ContractCheck;

/// <summary>Implements <c>Dbc.Def</c> and <c>Dbc.DefSetter</c>: compiles all clauses of one call, then registers them together.</summary>
internal static class ContractDefinitions
{
    public static void Define(LambdaExpression member, LambdaExpression[] clauses, bool setter)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(clauses);
        for (int i = 0; i < clauses.Length; i++)
        {
            if (clauses[i] is null)
            {
                throw new ArgumentNullException(nameof(clauses), $"Contract clause {i} is null.");
            }
        }

        Type interfaceType = member.Parameters[0].Type;
        if (!interfaceType.IsInterface)
        {
            throw DefinitionErrors.Create(interfaceType, "contracts can only be defined for interfaces, because Dbc.Make proxies interfaces");
        }

        if (!setter && ClauseCompiler.AsContractCall(member.Body) is { } call)
        {
            if (call.Method.Name != nameof(Contract.Invariant))
            {
                throw DefinitionErrors.Create(
                    interfaceType,
                    member,
                    $"a Dbc.Def without a member selector can only define invariants; attach Contract.{call.Method.Name} to a member, e.g. Dbc.Def((IFoo x, int a) => x.M(a), (IFoo x, int a) => Contract.{call.Method.Name}(...))");
            }

            List<CompiledClause> invariants = [.. clauses.Prepend(member).Select(clause => ClauseCompiler.Compile(clause, interfaceType, member: null))];
            ContractRegistry.For(interfaceType).AddInvariants(invariants);
            return;
        }

        MemberSignature signature = MemberSelector.Select(member, setter);
        List<CompiledClause> compiled = [.. clauses.Select(clause => ClauseCompiler.Compile(clause, interfaceType, signature))];
        if (compiled.Count > 0)
        {
            ContractRegistry.For(interfaceType).AddMemberClauses(signature.Method, compiled);
        }
    }
}
