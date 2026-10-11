namespace ContractCheck;

/// <summary>Recognises the awaitable return types whose postconditions ContractCheck checks on completion.</summary>
internal static class Awaitables
{
    /// <summary>
    /// Returns <see langword="true"/> for <see cref="Task"/>, <see cref="Task{TResult}"/>, <see cref="ValueTask"/> and
    /// <see cref="ValueTask{TResult}"/>; <paramref name="awaitedType"/> is the result type, or <see langword="null"/> for non-generic tasks.
    /// </summary>
    public static bool IsAwaitable(Type type, out Type? awaitedType)
    {
        awaitedType = null;
        if (type == typeof(Task) || type == typeof(ValueTask))
        {
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() is var definition
            && (definition == typeof(Task<>) || definition == typeof(ValueTask<>)))
        {
            awaitedType = type.GetGenericArguments()[0];
            return true;
        }

        return false;
    }
}
