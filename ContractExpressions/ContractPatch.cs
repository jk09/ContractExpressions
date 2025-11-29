using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;

namespace ContractExpressions;

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


}
