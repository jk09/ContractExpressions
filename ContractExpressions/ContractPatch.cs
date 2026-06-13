using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ContractExpressions;

internal static class ContractPatch
{
    // Raises the ContractFailed event and, if the failure was not handled by a subscriber,
    // calls TriggerFailure to throw ContractException. This ensures contracts are enforced
    // even when no Contract.ContractFailed handler is registered.
    private static void RaiseContractFailed(ContractFailureKind failureKind, string? userMessage, string? conditionText, Exception? innerException)
    {
        var displayMessage = ContractHelper.RaiseContractFailedEvent(failureKind, userMessage, conditionText, innerException);
        if (displayMessage != null)
        {
            ContractHelper.TriggerFailure(failureKind, displayMessage, userMessage, conditionText, innerException);
        }
    }

    public static void Assert(bool condition, string? message = null)
    {
        if (!condition)
        {
            RaiseContractFailed(ContractFailureKind.Assert, message, null, null);
        }
    }

    public static void Assume(bool condition, string? message = null)
    {
        if (!condition)
        {
            RaiseContractFailed(ContractFailureKind.Assume, message, null, null);
        }
    }

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
                Exception ex;

                // Special handling for ArgumentNullException - it needs null paramName and message as second arg
                if (exceptionType == typeof(ArgumentNullException))
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
                                exceptionType,
                                BindingFlags.Public | BindingFlags.Instance, default(Binder),
                                new object?[] { message }, CultureInfo.InstalledUICulture)!;
                    }
                    catch
                    {
                        // Fall back to parameterless constructor
                        ex = (Exception)Activator.CreateInstance(exceptionType)!;
                    }
                }

                throw ex;

            }
            else
            {
                RaiseContractFailed(ContractFailureKind.Precondition, message, null, null);
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
                RaiseContractFailed(ContractFailureKind.Postcondition, message, null, null);
            }
        }
    }

    public static void EnsuresOnThrow<TException>(bool condition, string? message = null) where TException : Exception
    {
        if (!condition)
        {
            RaiseContractFailed(ContractFailureKind.PostconditionOnException, message, null, null);
        }
    }

    public static void Invariant(bool condition, string? message = null)
    {
        if (!condition)
        {
            RaiseContractFailed(ContractFailureKind.Invariant, message, null, null);
        }
    }


}
