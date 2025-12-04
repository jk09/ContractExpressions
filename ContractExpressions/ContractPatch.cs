using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ContractExpressions;

internal static class ContractPatch
{
    public static T Result<T>(ContractContext context)
    {
        return Cast<T>(context.Result);
    }

    public static T OldValue<T>(PropertyInfo property, ContractContext context)
    {
        return Cast<T>(context.OldValues?[property]);
    }

    public static T? ValueAtReturn<T>(ParameterInfo parameter, ContractContext context)
    {
        throw new NotSupportedException("ValueAtReturn is not supported in this context.");
    }

    private static T Cast<T>(object? value)
    {
        return value is T tValue ? tValue : default(T)!;
    }


    public static void Requires(bool condition, string? message = null, Type? exceptionType = null)
    {
        if (!condition)
        {
            if (exceptionType != null)
            {
                var ex = Activator.CreateInstance(
                            exceptionType,
                            BindingFlags.Public | BindingFlags.Instance, default(Binder),
                            new object?[] { message, new Exception() }, CultureInfo.InstalledUICulture);

                throw (Exception)ex!;

            }
            else
            {
                throw new ContractViolationException(ContractFailureKind.Precondition, message);
            }
        }
    }

    public static void Ensures(bool condition, string? message = null, Type? exceptionType = null)
    {
        if (!condition)
        {
            if (exceptionType != null)
            {
                var ex = Activator.CreateInstance(
                            exceptionType,
                            BindingFlags.Public | BindingFlags.Instance, default(Binder),
                            new object?[] { message, new Exception() }, CultureInfo.InstalledUICulture);

                throw (Exception)ex!;

            }
            else
            {
                throw new ContractViolationException(ContractFailureKind.Postcondition, message);
            }
        }
    }

    public static void EnsuresOnThrow<TException>(bool condition, string? message = null) where TException : Exception
    {
        // EnsuresOnThrow is checked when the specified exception is thrown
        // In runtime, this is typically a no-op unless the exception is actually thrown
        // Implementation would require exception filtering which is complex
        // For now, we'll implement it as a standard postcondition check
        if (!condition)
        {
            throw new ContractViolationException(ContractFailureKind.PostconditionOnException, message);
        }
    }

    public static void Requires<TException>(bool condition, string? message = null) where TException : Exception
    {
        if (!condition)
        {
            Exception ex;

            // Special handling for ArgumentNullException - it needs null paramName and message as second arg
            if (typeof(TException) == typeof(ArgumentNullException))
            {
                // ArgumentNullException(string? message) uses message as paramName, not message
                // So we need to create it differently - use reflection to call the right constructor
                // or just create with message only which puts it as Message property indirectly
                ex = new ArgumentNullException(null, message);
            }
            else
            {
                // Try standard constructor with message
                try
                {
                    ex = (Exception)Activator.CreateInstance(
                            typeof(TException),
                            BindingFlags.Public | BindingFlags.Instance, default(Binder),
                            new object?[] { message }, CultureInfo.InstalledUICulture)!;
                }
                catch
                {
                    // Fall back to parameterless constructor
                    ex = (Exception)Activator.CreateInstance(typeof(TException))!;
                }
            }

            throw ex;
        }
    }

    public static void Assert(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new ContractViolationException(ContractFailureKind.Assert, message);
        }
    }

    public static void Assume(bool condition, string? message = null)
    {
        // Assume is primarily for static analysis tools
        // At runtime, it behaves similarly to Assert
        if (!condition)
        {
            throw new ContractViolationException(ContractFailureKind.Assume, message);
        }
    }

    public static void Invariant(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new ContractViolationException(ContractFailureKind.Invariant, message);
        }
    }

    public static bool ForAll<T>(IEnumerable<T> collection, Predicate<T> predicate)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        foreach (T item in collection)
        {
            if (!predicate(item))
                return false;
        }
        return true;
    }

    public static bool ForAll(int fromInclusive, int toExclusive, Predicate<int> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        if (toExclusive < fromInclusive)
            throw new ArgumentException("toExclusive must be greater than or equal to fromInclusive");

        for (int i = fromInclusive; i < toExclusive; i++)
        {
            if (!predicate(i))
                return false;
        }
        return true;
    }

    public static bool Exists<T>(IEnumerable<T> collection, Predicate<T> predicate)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        foreach (T item in collection)
        {
            if (predicate(item))
                return true;
        }
        return false;
    }

    public static bool Exists(int fromInclusive, int toExclusive, Predicate<int> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));
        if (toExclusive < fromInclusive)
            throw new ArgumentException("toExclusive must be greater than or equal to fromInclusive");

        for (int i = fromInclusive; i < toExclusive; i++)
        {
            if (predicate(i))
                return true;
        }
        return false;
    }

    public static void EndContractBlock()
    {
        // This method marks the end of the contract section when a method's contracts
        // contain only preconditions in the if-then-throw form.
        // It's primarily a marker for static analysis tools and has no runtime behavior.
    }

    // Event for contract failures
    public static event EventHandler<ContractFailedEventArgs>? ContractFailed;

    internal static void RaiseContractFailedEvent(ContractFailureKind failureKind, string? message, string? condition, Exception? innerException)
    {
        var handler = ContractFailed;
        if (handler != null)
        {
            var args = new ContractFailedEventArgs(failureKind, message, condition, innerException);
            handler(null, args);

            if (!args.Handled && !args.Unwind)
            {
                // If not handled, throw the exception
                throw new ContractViolationException(failureKind, message);
            }
        }
    }
}

public class ContractFailedEventArgs : EventArgs
{
    public ContractFailureKind FailureKind { get; }
    public string? Message { get; }
    public string? Condition { get; }
    public Exception? OriginalException { get; }
    public bool Handled { get; private set; }
    public bool Unwind { get; private set; }

    public ContractFailedEventArgs(ContractFailureKind failureKind, string? message, string? condition, Exception? originalException)
    {
        FailureKind = failureKind;
        Message = message;
        Condition = condition;
        OriginalException = originalException;
    }

    public void SetHandled()
    {
        Handled = true;
    }

    public void SetUnwind()
    {
        Unwind = true;
    }
}
