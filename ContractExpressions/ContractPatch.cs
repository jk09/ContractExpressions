using System.Diagnostics.Contracts;
using System.Reflection;
internal static class ContractPatch
{
    public static T? Result<T>(ContractContext context)
    {
        return context.Result is T value ? value : default(T);
    }

    public static T? OldValue<T>(PropertyInfo property, ContractContext context)
    {
        return context?.OldValues?[property] is T value ? value : default(T);
    }

    public static void Requires(bool condition, string? message = null, Type? exceptionType = null)
    {
        if (!condition)
        {
            if (exceptionType != null)
            {
                throw (Exception)Activator.CreateInstance(exceptionType, message)!;
            }
            else
            {
                throw new ContractViolationException(ContractFailureKind.Precondition, message);
            }
        }
    }

    public static void Ensures(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new ContractViolationException(ContractFailureKind.Postcondition, message);
        }
    }

    public static void Ensures<TException>(bool condition, string? message = null) where TException : Exception
    {
        if (!condition)
        {
            throw (TException)Activator.CreateInstance(typeof(TException), message)!;
        }
    }
}
