using System.Collections.Immutable;
using System.Reflection;

namespace ContractCheck;

/// <summary>
/// Runs one intercepted call: entry checks → old values → target → (on exception) matching <c>EnsuresOnThrow</c>
/// → postconditions → member invariants → interface invariants. Async members finish the exit checks when their task completes.
/// </summary>
internal static class ContractInvoker
{
    public static object? Invoke(Type interfaceType, object target, MethodInfo method, object?[] arguments)
    {
        MemberContracts contracts = MemberContracts.For(interfaceType, method);
        if (!contracts.HasChecks)
        {
            return method.Invoke(target, BindingFlags.DoNotWrapExceptions, binder: null, arguments, culture: null);
        }

        var context = new InvocationContext(target, arguments);
        CheckAll(contracts.EntryChecks, context, contracts);
        contracts.CaptureOldValues(context);

        object? result;
        try
        {
            result = method.Invoke(target, BindingFlags.DoNotWrapExceptions, binder: null, arguments, culture: null);
        }
        catch (Exception exception)
        {
            CheckOnThrow(contracts, context, exception);
            throw;
        }

        context.Result = result;
        CheckAll(contracts.ExitChecks, context, contracts);
        return contracts.WrapsReturnedTask ? AsyncContracts.Wrap(result, new AsyncCompletion(contracts, context)) : result;
    }

    /// <summary>Checks the interface invariants of a new proxy; failures report <see cref="ContractViolationException.CreationMethodName"/>.</summary>
    public static void CheckCreation(Type interfaceType, object target)
    {
        ImmutableArray<BoundClause> invariants = MemberContracts.CreationInvariants(interfaceType);
        if (invariants.IsEmpty)
        {
            return;
        }

        var context = new InvocationContext(target, []);
        foreach (BoundClause invariant in invariants)
        {
            Check(invariant, context, interfaceType, member: null, thrownException: null);
        }
    }

    /// <summary>Checks every <c>EnsuresOnThrow&lt;TException&gt;</c> clause whose <c>TException</c> matches <paramref name="exception"/>.</summary>
    public static void CheckOnThrow(MemberContracts contracts, InvocationContext context, Exception exception)
    {
        foreach (BoundClause clause in contracts.OnThrowChecks)
        {
            if (clause.Clause.ExceptionType!.IsInstanceOfType(exception))
            {
                Check(clause, context, contracts.InterfaceType, contracts.Member, exception);
            }
        }
    }

    public static void CheckAll(ImmutableArray<BoundClause> clauses, InvocationContext context, MemberContracts contracts)
    {
        foreach (BoundClause clause in clauses)
        {
            Check(clause, context, contracts.InterfaceType, contracts.Member, thrownException: null);
        }
    }

    private static void Check(in BoundClause bound, InvocationContext context, Type interfaceType, MethodInfo? member, Exception? thrownException)
    {
        CompiledClause clause = bound.Clause;
        context.CurrentOldValues = bound.OldValueIndex >= 0 ? context.OldValues![bound.OldValueIndex] : null;

        bool holds;
        try
        {
            holds = clause.Predicate(context);
        }
        catch (Exception exception)
        {
            // A condition that throws is a defect of the contract (or of a guard missing from it); say which clause it was.
            Annotate(exception, clause);
            throw;
        }

        if (holds)
        {
            return;
        }

        FailureReporter.Report(new ContractFailure(
            clause.FailureKind,
            clause.ConditionText,
            EvaluateMessage(clause, context),
            clause.ContractText,
            interfaceType,
            member,
            thrownException,
            clause.Kind == ClauseKind.RequiresOfException ? clause.ExceptionType : null));
    }

    private static string? EvaluateMessage(CompiledClause clause, InvocationContext context)
    {
        try
        {
            return clause.Message?.Invoke(context);
        }
        catch (Exception exception)
        {
            return $"<the clause message threw {exception.GetType().Name}: {exception.Message}>";
        }
    }

    private static void Annotate(Exception exception, CompiledClause clause)
    {
        try
        {
            exception.Data[ContractFailureExtensions.ClauseKey] = clause.ContractText;
        }
        catch (NotSupportedException)
        {
            // Read-only Data dictionary: rethrow without the annotation.
        }
    }
}
