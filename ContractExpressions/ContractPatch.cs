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
}
