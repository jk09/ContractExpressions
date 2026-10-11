using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace ContractCheck;

/// <summary>What to check when the task returned by an async member finishes.</summary>
internal sealed class AsyncCompletion(MemberContracts contracts, InvocationContext context)
{
    /// <summary>The member's declared return type: <see cref="Task"/>, <see cref="Task{TResult}"/>, <see cref="ValueTask"/> or <see cref="ValueTask{TResult}"/>.</summary>
    public Type ReturnType => contracts.Member.ReturnType;

    public void Succeeded(object? result)
    {
        context.Result = result;
        ContractInvoker.CheckAll(contracts.CompletionChecks, context, contracts);
    }

    public void Failed(Exception exception) => ContractInvoker.CheckOnThrow(contracts, context, exception);
}

/// <summary>
/// Wraps the <see cref="Task"/>, <see cref="Task{TResult}"/>, <see cref="ValueTask"/> or <see cref="ValueTask{TResult}"/>
/// returned by an async member in one that completes only after the member's postconditions, invariants and
/// <c>EnsuresOnThrow</c> clauses were checked; a failed check faults the returned task with the contract failure.
/// </summary>
internal static class AsyncContracts
{
    private static readonly ConcurrentDictionary<Type, Func<object, AsyncCompletion, object>> Wrappers = new();

    /// <summary>Returns a task of the member's declared return type that completes after the deferred checks.</summary>
    public static object? Wrap(object? returnedTask, AsyncCompletion completion) =>
        returnedTask is null ? null : Wrappers.GetOrAdd(completion.ReturnType, CreateWrapper)(returnedTask, completion);

    private static Func<object, AsyncCompletion, object> CreateWrapper(Type awaitableType)
    {
        MethodInfo method = awaitableType == typeof(Task) ? Method(nameof(WrapTask))
            : awaitableType == typeof(ValueTask) ? Method(nameof(WrapValueTask))
            : awaitableType.GetGenericTypeDefinition() == typeof(Task<>) ? Method(nameof(WrapTaskOf)).MakeGenericMethod(awaitableType.GetGenericArguments())
            : Method(nameof(WrapValueTaskOf)).MakeGenericMethod(awaitableType.GetGenericArguments());

        ParameterExpression task = Expression.Parameter(typeof(object), "task");
        ParameterExpression completion = Expression.Parameter(typeof(AsyncCompletion), "completion");
        Expression call = Expression.Call(method, Expression.Convert(task, awaitableType), completion);
        return Expression.Lambda<Func<object, AsyncCompletion, object>>(Expression.Convert(call, typeof(object)), task, completion).Compile();
    }

    private static MethodInfo Method(string name) =>
        typeof(AsyncContracts).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static) ?? throw new MissingMethodException(nameof(AsyncContracts), name);

    private static async Task WrapTask(Task task, AsyncCompletion completion)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            completion.Failed(exception);
            throw;
        }

        completion.Succeeded(null);
    }

    private static async Task<T> WrapTaskOf<T>(Task<T> task, AsyncCompletion completion)
    {
        T result;
        try
        {
            result = await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            completion.Failed(exception);
            throw;
        }

        completion.Succeeded(result);
        return result;
    }

    private static async ValueTask WrapValueTask(ValueTask task, AsyncCompletion completion)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            completion.Failed(exception);
            throw;
        }

        completion.Succeeded(null);
    }

    private static async ValueTask<T> WrapValueTaskOf<T>(ValueTask<T> task, AsyncCompletion completion)
    {
        T result;
        try
        {
            result = await task.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            completion.Failed(exception);
            throw;
        }

        completion.Succeeded(result);
        return result;
    }
}
