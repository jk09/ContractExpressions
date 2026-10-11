using System.Diagnostics.Contracts;
using System.Reflection;

namespace ContractCheck;

/// <summary>
/// Creates the exception thrown by a failed <c>Contract.Requires&lt;TException&gt;</c> clause, using the constructor
/// rules of the CodeContracts rewriter: a <c>(paramName, message)</c> constructor receives the user message as the
/// parameter name (so <c>Requires&lt;ArgumentNullException&gt;(x != null, "x")</c> sets <c>ParamName</c> to <c>"x"</c>),
/// otherwise the <c>(message)</c> constructor receives the failure message.
/// </summary>
internal static class TypedExceptionFactory
{
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

    /// <summary>Returns <see langword="null"/> when <paramref name="exceptionType"/> can be created, otherwise the reason it cannot.</summary>
    public static string? WhyNotConstructible(Type exceptionType)
    {
        if (exceptionType.IsAbstract)
        {
            return $"'{TypeNames.Format(exceptionType)}' is abstract";
        }

        if (exceptionType.ContainsGenericParameters)
        {
            return $"'{TypeNames.Format(exceptionType)}' is an open generic type";
        }

        bool usable = ParamNameMessageConstructor(exceptionType) is not null
            || exceptionType.GetConstructor(PublicInstance, null, [typeof(string)], null) is not null
            || exceptionType.GetConstructor(PublicInstance, null, Type.EmptyTypes, null) is not null;

        return usable ? null : $"'{TypeNames.Format(exceptionType)}' has no public (string paramName, string message), (string message) or parameterless constructor";
    }

    public static Exception Create(Type exceptionType, string displayMessage, string? userMessage, string? condition)
    {
        Exception exception;
        if (ParamNameMessageConstructor(exceptionType) is { } paramNameCtor)
        {
            exception = (Exception)paramNameCtor.Invoke([userMessage, displayMessage]);
        }
        else if (exceptionType.GetConstructor(PublicInstance, null, [typeof(string)], null) is { } messageCtor)
        {
            exception = (Exception)messageCtor.Invoke([displayMessage]);
        }
        else
        {
            exception = (Exception)Activator.CreateInstance(exceptionType)!;
        }

        Mark(exception, ContractFailureKind.Precondition, condition);
        return exception;
    }

    private static ConstructorInfo? ParamNameMessageConstructor(Type exceptionType)
    {
        ConstructorInfo? ctor = exceptionType.GetConstructor(PublicInstance, null, [typeof(string), typeof(string)], null);
        return ctor is not null && ctor.GetParameters()[0].Name == "paramName" ? ctor : null;
    }

    private static void Mark(Exception exception, ContractFailureKind kind, string? condition)
    {
        try
        {
            exception.Data[ContractFailureExtensions.FailureKindKey] = kind;
            exception.Data[ContractFailureExtensions.ConditionKey] = condition;
        }
        catch (NotSupportedException)
        {
            // Exception types may expose a read-only Data dictionary; the exception is still thrown, just unmarked.
        }
    }
}
