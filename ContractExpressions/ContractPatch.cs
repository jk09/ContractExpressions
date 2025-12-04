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
        return Cast<T?>(context.ValuesAtReturn?[parameter]);
    }

    private static T Cast<T>(object? value)
    {
        return value is T tValue ? tValue : default(T);
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
