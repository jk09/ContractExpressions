using System.Reflection;
using FsCheck;
using FsCheck.Fluent;

namespace ContractCheck.FsCheck;

/// <summary>
/// FsCheck properties that use ContractCheck contracts as the test oracle. Call them from an FsCheck
/// <c>[Property]</c> method, passing the generated arguments into <c>invoke</c>:
/// <code>
/// [Property]
/// public Property Withdraw_keeps_contracts(int amount) =&gt;
///     DbcPropertyTest.Check(() =&gt; Dbc.Make&lt;IAccount&gt;(new Account(100)), account =&gt; account.Withdraw(amount));
/// </code>
/// For each generated case:
/// <list type="bullet">
/// <item>the factory creates a fresh proxy (an interface invariant that fails at creation fails the property);</item>
/// <item>an entry-check failure – <c>Requires</c>, <c>Requires&lt;TException&gt;</c>, <c>Assert</c>, <c>Assume</c> – discards the case: the generated input is outside the contract;</item>
/// <item>a postcondition, <c>EnsuresOnThrow</c> or invariant failure, or any other exception, fails the property and FsCheck shrinks the input;</item>
/// <item>tasks returned by <c>invoke</c> are awaited, so the postconditions of async members are checked too.</item>
/// </list>
/// <c>Check</c> awaits whatever <c>invoke</c> returns (<see cref="Task"/>, <see cref="ValueTask"/> or
/// <see cref="ValueTask{TResult}"/>); <c>CheckAsync</c> takes <see cref="Task"/>-returning delegates only, so
/// <c>async</c> lambdas bind to it unambiguously on every C# version.
/// </summary>
public static class DbcPropertyTest
{
    private static readonly MethodInfo AsTaskOfValueTask = typeof(DbcPropertyTest).GetMethod(nameof(AsTask), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>Checks one call of a member that returns a value (tasks are awaited).</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="TResult">The member's return type.</typeparam>
    /// <param name="proxyFactory">Creates a fresh proxy, usually <c>() =&gt; Dbc.Make&lt;T&gt;(new Impl())</c>.</param>
    /// <param name="invoke">Calls the member under test with the generated arguments.</param>
    public static Property Check<T, TResult>(Func<T> proxyFactory, Func<T, TResult> invoke)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(invoke);
        return Run(proxyFactory, proxy => Await(invoke(proxy)));
    }

    /// <summary>Checks one call of a <see langword="void"/> member.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <param name="proxyFactory">Creates a fresh proxy, usually <c>() =&gt; Dbc.Make&lt;T&gt;(new Impl())</c>.</param>
    /// <param name="invoke">Calls the member under test with the generated arguments.</param>
    public static Property Check<T>(Func<T> proxyFactory, Action<T> invoke)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(invoke);
        return Run(proxyFactory, invoke);
    }

    /// <summary>Checks one call of an async member; the returned task is awaited so deferred postconditions run.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <param name="proxyFactory">Creates a fresh proxy.</param>
    /// <param name="invoke">Calls the async member under test.</param>
    public static Property CheckAsync<T>(Func<T> proxyFactory, Func<T, Task> invoke)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(invoke);
        return Run(proxyFactory, proxy => invoke(proxy).GetAwaiter().GetResult());
    }

    /// <summary>Checks one call of an async member that returns a value; the task is awaited so deferred postconditions run.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <typeparam name="TResult">The awaited result type.</typeparam>
    /// <param name="proxyFactory">Creates a fresh proxy.</param>
    /// <param name="invoke">Calls the async member under test.</param>
    public static Property CheckAsync<T, TResult>(Func<T> proxyFactory, Func<T, Task<TResult>> invoke)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(invoke);
        return Run(proxyFactory, proxy => invoke(proxy).GetAwaiter().GetResult());
    }

    /// <summary>
    /// Runs a generated sequence of operations against one proxy, so invariants and postconditions are checked across state
    /// changes. Steps rejected by an entry check are skipped; any other contract failure fails the property.
    /// </summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <param name="proxyFactory">Creates the proxy the steps share.</param>
    /// <param name="steps">The operations, typically built from generated data, e.g. <c>amounts.Select(a =&gt; (Action&lt;IAccount&gt;)(x =&gt; x.Deposit(a)))</c>.</param>
    public static Property CheckSequence<T>(Func<T> proxyFactory, IEnumerable<Action<T>> steps)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(proxyFactory);
        ArgumentNullException.ThrowIfNull(steps);

        T proxy;
        try
        {
            proxy = proxyFactory();
        }
        catch (Exception exception) when (exception.IsEntryCheckFailure())
        {
            return Discarded();
        }

        int executed = 0;
        int skipped = 0;
        foreach (Action<T> step in steps)
        {
            try
            {
                step(proxy);
                executed++;
            }
            catch (Exception exception) when (exception.IsEntryCheckFailure())
            {
                skipped++;
            }
        }

        Property result = Prop.Classify(Prop.ToProperty(true), skipped > 0, "steps skipped by entry checks");
        return Prop.Classify(result, executed == 0, "no step ran");
    }

    /// <summary>Like <see cref="CheckSequence{T}(Func{T}, IEnumerable{Action{T}})"/> for async operations; each step is awaited before the next.</summary>
    /// <typeparam name="T">The contract interface.</typeparam>
    /// <param name="proxyFactory">Creates the proxy the steps share.</param>
    /// <param name="steps">The async operations.</param>
    public static Property CheckSequenceAsync<T>(Func<T> proxyFactory, IEnumerable<Func<T, Task>> steps)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(steps);
        return CheckSequence(proxyFactory, steps.Select(step => (Action<T>)(proxy => step(proxy).GetAwaiter().GetResult())));
    }

    private static Property Run<T>(Func<T> proxyFactory, Action<T> invoke)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(proxyFactory);
        try
        {
            invoke(proxyFactory());
        }
        catch (Exception exception) when (exception.IsEntryCheckFailure())
        {
            return Discarded();
        }

        return Prop.ToProperty(true);
    }

    private static Property Discarded() => Prop.When(false, () => true);

    /// <summary>Blocks on a returned task, so async postconditions are checked within the generated case.</summary>
    private static void Await(object? result)
    {
        switch (result)
        {
            case Task task:
                task.GetAwaiter().GetResult();
                break;
            case ValueTask valueTask:
                valueTask.GetAwaiter().GetResult();
                break;
            case not null when result.GetType() is { IsGenericType: true } type && type.GetGenericTypeDefinition() == typeof(ValueTask<>):
                ((Task)AsTaskOfValueTask.MakeGenericMethod(type.GetGenericArguments()).Invoke(null, [result])!).GetAwaiter().GetResult();
                break;
        }
    }

    private static Task AsTask<TResult>(ValueTask<TResult> valueTask) => valueTask.AsTask();
}
